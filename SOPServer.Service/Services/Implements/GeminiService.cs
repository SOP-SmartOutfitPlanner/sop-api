using AutoMapper;
using GenerativeAI;
using GenerativeAI.Tools;
using GenerativeAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.QDrantModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SOPServer.Service.Services.Implements
{
    public class GeminiService : IGeminiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GenerativeModel _generativeModel;
        private readonly EmbeddingModel _embeddingModel;
        private readonly GenerativeModel _suggestionAiClient;
        private readonly QDrantClientSettings _qdrantClientSettings;
        private readonly ILogger<GeminiService> _logger;
        private readonly HashSet<string> _allowedMime = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };


        public GeminiService(IOptions<GeminiSettings> geminiSettings, IUnitOfWork unitOfWork, IOptions<QDrantClientSettings> qdrantClientSettings, ILogger<GeminiService> logger)
        {
            _unitOfWork = unitOfWork;
            _qdrantClientSettings = qdrantClientSettings.Value;
            _logger = logger;

            // AI model for item analyzing
            var apiKeyAnalyzing = GetAiSettingValue(AISettingType.API_ITEM_ANALYZING);
            var modelIdAnalyzing = GetAiSettingValue(AISettingType.MODEL_ANALYZING);
            var generativeAiClient = new GoogleAi(apiKeyAnalyzing);
            _generativeModel = generativeAiClient.CreateGenerativeModel(modelIdAnalyzing);

            // Embedding model
            var apiKeyEmbedding = GetAiSettingValue(AISettingType.API_EMBEDDING);
            var modelIdEmbedding = GetAiSettingValue(AISettingType.MODEL_EMBEDDING);
            var embeddingAiClient = new GoogleAi(apiKeyEmbedding);
            _embeddingModel = embeddingAiClient.CreateEmbeddingModel(modelIdEmbedding);

            var apiKeySuggest = GetAiSettingValue(AISettingType.API_SUGGESTION);
            var modelSuggestion = GetAiSettingValue(AISettingType.MODEL_SUGGESTION);
            var suggestionAiClient = new GoogleAi(apiKeySuggest);
            _suggestionAiClient = suggestionAiClient.CreateGenerativeModel(modelSuggestion);
        }

        private string GetAiSettingValue(AISettingType type)
        {
            var setting = _unitOfWork.AISettingRepository
                                    .GetByTypeAsync(type)
                                    .GetAwaiter()
                                    .GetResult();

            if (setting == null)
            {
                throw new InvalidOperationException($"AI Setting '{type}' not found in database");
            }

            if (string.IsNullOrWhiteSpace(setting.Value))
            {
                throw new InvalidOperationException($"AI Setting '{type}' has no value configured");
            }

            return setting.Value;
        }

        public async Task<ItemModelAI?> ImageGenerateContent(string base64Image, string mimeType, string prompt)
        {
            var generateRequest = new GenerateContentRequest();
            generateRequest.AddInlineData(base64Image, mimeType);
            generateRequest.UseJsonMode<ItemModelAI>();
            generateRequest.AddText(prompt);

            const int maxRetryAttempts = 5;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("ImageGenerateContent: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    var result = await _generativeModel.GenerateObjectAsync<ItemModelAI>(generateRequest);

                    var missingFields = CheckMissingField(result);

                    if (missingFields.Any())
                    {
                        _logger.LogWarning("ImageGenerateContent: Attempt {Attempt} returned incomplete data. Missing fields: {MissingFields}",
                            attempt, string.Join(", ", missingFields));

                        if (attempt == maxRetryAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.IMAGE_ANALYSIS_FAILED}: AI model returned incomplete data. Missing: {string.Join(", ", missingFields)}");
                        }
                        continue;
                    }

                    _logger.LogInformation("ImageGenerateContent: Successfully generated content on attempt {Attempt}", attempt);
                    return result;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ImageGenerateContent: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.IMAGE_ANALYSIS_FAILED}: {ex.Message}");
                    }
                }
            }

            throw new BadRequestException(MessageConstants.IMAGE_ANALYSIS_FAILED);
        }
        private List<string> CheckMissingField(ItemModelAI result)
        {
            var missingFields = new List<string>();
            if (result == null)
            {
                missingFields.Add("result");
            }
            else
            {
                if (result.Colors == null || !result.Colors.Any())
                    missingFields.Add("Colors");
                if (string.IsNullOrWhiteSpace(result.AiDescription))
                    missingFields.Add("AiDescription");
                if (string.IsNullOrWhiteSpace(result.WeatherSuitable))
                    missingFields.Add("WeatherSuitable");
                if (string.IsNullOrWhiteSpace(result.Condition))
                    missingFields.Add("Condition");
                if (string.IsNullOrWhiteSpace(result.Pattern))
                    missingFields.Add("Pattern");
                if (string.IsNullOrWhiteSpace(result.Fabric))
                    missingFields.Add("Fabric");
                if (result.Styles == null)
                    missingFields.Add("Styles");
                if (result.Occasions == null)
                    missingFields.Add("Occasions");
                if (result.Seasons == null)
                    missingFields.Add("Seasons");
            }
            return missingFields;
        }

        public async Task<ImageValidation> ImageValidation(string base64Image, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType) || !_allowedMime.Contains(mimeType))
            {
                throw new NotFoundException(MessageConstants.MIMETYPE_NOT_VALID);
            }

            var validatePromptSetting = await _unitOfWork.AISettingRepository.GetByTypeAsync(AISettingType.VALIDATE_ITEM_PROMPT);
            var generateRequest = new GenerateContentRequest();
            generateRequest.AddText(validatePromptSetting.Value);
            generateRequest.AddInlineData(base64Image, mimeType);
            generateRequest.UseJsonMode<ImageValidation>();

            const int maxRetryAttempts = 5;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("ImageValidation: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    var result = await _generativeModel.GenerateObjectAsync<ImageValidation>(generateRequest);

                    _logger.LogInformation("ImageValidation: Successfully validated on attempt {Attempt}. IsValid: {IsValid}", attempt, result?.IsValid);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ImageValidation: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.IMAGE_VALIDATION_FAILED}: {ex.Message}");
                    }

                }
            }

            throw new BadRequestException(MessageConstants.IMAGE_VALIDATION_FAILED);
        }

        public async Task<List<float>?> EmbeddingText(string textEmbeeding)
        {
            var request = new EmbedContentRequest
            {
                Content = new Content { Parts = { new Part { Text = textEmbeeding } } },
                OutputDimensionality = int.Parse(_qdrantClientSettings.Size)
            };
            var response = await _embeddingModel.EmbedContentAsync(request);
            return response.Embedding.Values;
        }

        public async Task<CategoryItemAnalysisModel?> AnalyzingCategory(string base64Image, string mimeType, string finalPrompt)
        {
            var generateRequest = new GenerateContentRequest();
            generateRequest.AddInlineData(base64Image, mimeType);
            generateRequest.UseJsonMode<CategoryItemAnalysisModel>();
            generateRequest.AddText(finalPrompt);
            return await _generativeModel.GenerateObjectAsync<CategoryItemAnalysisModel>(generateRequest);
        }

        public async Task OutfitSuggestion(string occasion, string usercharacteristic, long userId, QuickTools tools)
        {
            var systemParts = new List<Part>
            {
                new Part { Text = @"You are an expert fashion stylist AI integrated with a vector-based retrieval system (Qdrant).
Your task is to generate a complete outfit using the user’s characteristics and occasion.
Follow this workflow exactly:
Analyze the user info (characteristics, style preferences, occasion).
Use the search item:
First, search user items by calling the SearchSimilarityByUserId function.
If user items are not sufficient, call the SearchSimilarityItemSystem function to retrieve system items.
Select the most suitable items to form a coherent outfit.
Combine the chosen items into a final outfit (list of item IDs).
Explain shortly why these items were chosen (color harmony, style, season, occasion fit, etc.).
Return the result strictly as JSON matching the structure of OutfitSelectionModel: {
  ""itemIds"": [1, 2, 3],
  ""reason"": ""...""
} Do not include any extra text, no formatting, no explanation outside the JSON.
Output only the JSON of OutfitSelectionModel."
                },
            };

            var generateRequest = new GenerateContentRequest();

            _suggestionAiClient.AddFunctionTool(tools);

            generateRequest.SystemInstruction = new Content
            {
                Parts = systemParts
            };

            var userParts = new List<Part>
                    {
                        new Part { Text = usercharacteristic },
                        new Part { Text = "User id: " + userId  },
                    };

            if (!string.IsNullOrEmpty(occasion))
            {
                userParts.Add(new Part { Text = occasion });
            }

            var userContent = new Content { Parts = userParts, Role = "user" };
            var systemContent = new Content { Parts = systemParts, Role = "user" };
            generateRequest.AddContent(userContent);
            generateRequest.AddContent(systemContent);
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            var response = await _suggestionAiClient.GenerateContentAsync(generateRequest);
            sw.Stop();
            Console.WriteLine("Suggest Item Time: " + sw.ElapsedMilliseconds + "ms");
        }

        public async Task<List<string>> OutfitSuggestion(string occasion, string usercharacteristic)
        {
            var outfitPromptSetting = await _unitOfWork.AISettingRepository.GetByTypeAsync(AISettingType.OUTFIT_GENERATION_PROMPT);

            var systemParts = new List<Part>
            {
                    new Part { Text = outfitPromptSetting.Value }
            };

            var generateRequest = new GenerateContentRequest();

            generateRequest.SystemInstruction = new Content
            {
                Parts = systemParts
            };

            var userParts = new List<Part>
            {
                new Part { Text = $"User Characteristics: {usercharacteristic}" }
            };

            if (!string.IsNullOrEmpty(occasion))
            {
                userParts.Add(new Part { Text = $"Occasion: {occasion}" });
            }

            var userContent = new Content { Parts = userParts, Role = "user" };
            generateRequest.AddContent(userContent);

            generateRequest.UseJsonMode<List<string>>();

            const int maxRetryAttempts = 3;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("OutfitSuggestion: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    var result = await _suggestionAiClient.GenerateObjectAsync<List<string>>(generateRequest);

                    if (result == null || result.Count() == 0)
                    {
                        _logger.LogWarning("OutfitSuggestion: Attempt {Attempt} returned empty result", attempt);

                        if (attempt == maxRetryAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI model returned empty outfit suggestions");
                        }
                        continue;
                    }

                    _logger.LogInformation("OutfitSuggestion: Successfully generated {Count} item descriptions on attempt {Attempt}",
                  result.Count(), attempt);

                    return result;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OutfitSuggestion: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: {ex.Message}");
                    }
                }
            }

            throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
        }

        public async Task<OutfitSelectionModel> ChooseOutfit(string occasion, string usercharacteristic, List<QDrantSearchModels> items, QuickTools tools)
        {
            var choosePromptSetting = await _unitOfWork.AISettingRepository.GetByTypeAsync(AISettingType.OUTFIT_CHOOSE_PROMPT);

            // Convert items to JSON for the AI to understand
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var itemsJson = JsonSerializer.Serialize(items.Select(item => new
            {
                item.Id,
                item.ItemName,
                item.ImgURL,
                Colors = item.Colors,
                AiDescription = item.AiDescription,
                WeatherSuitable = item.WeatherSuitable,
                Condition = item.Condition,
                Pattern = item.Pattern,
                Fabric = item.Fabric,
                Styles = item.Styles,
                Occasions = item.Occasions,
                Seasons = item.Seasons,
                Score = item.Score
            }), serializerOptions);

            var systemParts = new List<Part>
            {
                new Part { Text = choosePromptSetting.Value }
            };

            var generateRequest = new GenerateContentRequest();

            // Add function tool for searching system items if needed
            _suggestionAiClient.AddFunctionTool(tools);

            generateRequest.SystemInstruction = new Content
            {
                Parts = systemParts
            };

            var userParts = new List<Part>
            {
                new Part { Text = $"User Characteristics: {usercharacteristic}" },
                new Part { Text = $"Available Items: {itemsJson}" }
            };

            if (!string.IsNullOrEmpty(occasion))
            {
                userParts.Add(new Part { Text = $"Occasion: {occasion}" });
            }

            var userContent = new Content { Parts = userParts, Role = "user" };
            generateRequest.AddContent(userContent);

            const int maxRetryAttempts = 5;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("ChooseOutfit: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    var response = await _suggestionAiClient.GenerateContentAsync(generateRequest);
                    
                    var result = JsonSerializer.Deserialize<OutfitSelectionModel>(response.Text);

                    if (result == null || result.ItemIds == null || !result.ItemIds.Any())
                    {
                        _logger.LogWarning("ChooseOutfit: Attempt {Attempt} returned empty result", attempt);

                        if (attempt == maxRetryAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI model returned empty outfit selection");
                        }
                        continue;
                    }

                    _logger.LogInformation("ChooseOutfit: Successfully selected {Count} items on attempt {Attempt}. Reason: {Reason}",
                              result.ItemIds.Count, attempt, result.Reason);

                    return result;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChooseOutfit: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: {ex.Message}");
                    }
                }
            }

            throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
        }
    }
}
