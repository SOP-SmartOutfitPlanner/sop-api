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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SOPServer.Service.Services.Implements
{
    public class GeminiService : IGeminiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GenerativeModel _generativeModel;
        private readonly EmbeddingModel _embeddingModel;
        private readonly QDrantClientSettings _qdrantClientSettings;
        private readonly ILogger<GeminiService> _logger;
        private readonly Lazy<IQdrantService> _qdrantService;
        private readonly HashSet<string> _allowedMime = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };
        private readonly GenerativeModel _suggestionModel;

        public GeminiService(IOptions<GeminiSettings> geminiSettings, IUnitOfWork unitOfWork, IOptions<QDrantClientSettings> qdrantClientSettings, ILogger<GeminiService> logger, Lazy<IQdrantService> qdrantService)
        {
            _unitOfWork = unitOfWork;
            _qdrantClientSettings = qdrantClientSettings.Value;
            _logger = logger;
            _qdrantService = qdrantService;

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

            var apiKeySuggestion = GetAiSettingValue(AISettingType.API_SUGGESTION);
            var modelIdSuggestiong = GetAiSettingValue(AISettingType.MODEL_SUGGESTION);
            var generativeAiClientSuggestion = new GoogleAi(apiKeySuggestion);
            _suggestionModel = generativeAiClientSuggestion.CreateGenerativeModel(modelIdSuggestiong);
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

        public async Task<List<string>> OutfitSuggestion(string occasion, string usercharacteristic, string? weather = null)
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

            if (!string.IsNullOrEmpty(weather))
            {
                userParts.Add(new Part { Text = $"Weather: {weather}" });
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

                    var result = await _generativeModel.GenerateObjectAsync<List<string>>(generateRequest);

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

        public async Task<OutfitSelectionModel> ChooseOutfit(string occasion, string usercharacteristic, List<string> searchResults, string? weather = null)
        {
            var choosePromptSetting = await _unitOfWork.AISettingRepository.GetByTypeAsync(AISettingType.OUTFIT_CHOOSE_PROMPT);

            // Format: ID|Category|Color|Style|Occasion|Season
            // Example: 123|Hoodie|Gray,Blue|Casual,Sporty|Home,Casual|Fall,Spring
            var itemsText = string.Join("\n", searchResults);

            var systemParts = new List<Part>
            {
                new Part { Text = choosePromptSetting.Value },
                new Part { Text = @"Item format is: ID|Category|Color|Style|Occasion|Season
Example: 123|Hoodie|Gray,Blue|Casual,Sporty|Home,Casual|Fall,Spring
Parse this compact format to make outfit decisions." }
            };

            var userParts = new List<Part>
            {
                new Part { Text = $"User Characteristics: {usercharacteristic}" },
                new Part { Text = $"Available Items:\n{itemsText}" }
            };

            if (!string.IsNullOrEmpty(occasion))
            {
                userParts.Add(new Part { Text = $"Occasion: {occasion}" });
            }
            else
            {
                userParts.Add(new Part { Text = $"Occasion: null" });
            }

            if (!string.IsNullOrEmpty(weather))
            {
                userParts.Add(new Part { Text = $"Weather: {weather}" });
            }

            // Step 1: Generate outfit selection response with tool calling
            var aiResponse = await GenerateOutfitSelectionResponseAsync(systemParts, userParts);

            // Step 2: Format response to JSON
            var result = await FormatOutfitSelectionToJsonAsync(aiResponse);

            return result;
        }

        private async Task<string> GenerateOutfitSelectionResponseAsync(List<Part> systemParts, List<Part> userParts)
        {
            const int maxRetryAttempts = 5;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("GenerateOutfitSelectionResponse: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    QuickTools tools = new QuickTools([_qdrantService.Value.SearchSimilarityItemSystem]);
                    var model = CreateSuggestionModel(tools);

                    var generateRequest = new GenerateContentRequest();
                    generateRequest.SystemInstruction = new Content
                    {
                        Parts = systemParts,
                        Role = "system"
                    };

                    var userContent = new Content { Parts = userParts, Role = "user" };
                    generateRequest.AddContent(userContent);

                    var response = await model.GenerateContentAsync(generateRequest);

                    if (string.IsNullOrWhiteSpace(response.Text))
                    {
                        _logger.LogWarning("GenerateOutfitSelectionResponse: Attempt {Attempt} returned empty response", attempt);

                        if (attempt == maxRetryAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI model returned empty response");
                        }

                        await Task.Delay(500);
                        continue;
                    }

                    _logger.LogInformation("GenerateOutfitSelectionResponse: Successfully generated response on attempt {Attempt}", attempt);
                    Console.WriteLine("OUTFIT RESPONSE: " + response.Text);

                    return response.Text;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GenerateOutfitSelectionResponse: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: {ex.Message}");
                    }

                    await Task.Delay(1000);
                }
            }

            throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
        }

        private async Task<OutfitSelectionModel> FormatOutfitSelectionToJsonAsync(string aiResponse)
        {
            const int maxRetryAttempts = 3;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("FormatOutfitSelectionToJson: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    var requestJsonMode = new GenerateContentRequest();
                    requestJsonMode.UseJsonMode<OutfitSelectionModel>();

                    var systemFormatParts = new List<Part>
                    {
                        new Part { Text = @"You must return only valid JSON in English. 
                                            Do not include any Vietnamese text even if the input contains Vietnamese.
                                            Do not output explanations outside the JSON object.

                                            Extract the item IDs from the previous AI outfit suggestion response.
                                            Do not modify, translate, or infer IDs — use them exactly as given.

                                            Return JSON in this structure:
                                            {
                                                ""itemIds"": [10012,32511],
                                                ""reason"": ""<≤50 words in English about color harmony, style match, and occasion fit. Do not mention item IDs.>""
                                            }" }
                    };

                    var aiResponsePart = new List<Part>
                    {
                        new Part { Text = aiResponse },
                    };

                    requestJsonMode.AddContent(new Content
                    {
                        Parts = aiResponsePart,
                        Role = "user"
                    });

                    requestJsonMode.SystemInstruction = new Content
                    {
                        Parts = systemFormatParts,
                        Role = "system"
                    };

                    var result = await _suggestionModel.GenerateObjectAsync<OutfitSelectionModel>(requestJsonMode);

                    if (result == null || result.ItemIds == null || !result.ItemIds.Any())
                    {
                        _logger.LogWarning("FormatOutfitSelectionToJson: Attempt {Attempt} returned empty result", attempt);

                        if (attempt == maxRetryAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI model returned empty outfit selection");
                        }

                        await Task.Delay(500);
                        continue;
                    }

                    _logger.LogInformation("FormatOutfitSelectionToJson: Successfully formatted {Count} items on attempt {Attempt}",
                        result.ItemIds.Count, attempt);

                    return result;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FormatOutfitSelectionToJson: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: Failed to format response - {ex.Message}");
                    }

                    await Task.Delay(500);
                }
            }

            throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
        }

        //private string CleanJsonResponse(string responseText)
        //{
        //    if (string.IsNullOrWhiteSpace(responseText))
        //    {
        //        return responseText;
        //    }

        //    var cleaned = responseText.Trim();

        //    // Remove ```json at the start
        //    if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        //    {
        //        cleaned = cleaned.Substring("```json".Length).TrimStart();
        //    }
        //    else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        //    {
        //        cleaned = cleaned.Substring("```".Length).TrimStart();
        //    }

        //    // Remove ``` at the end
        //    if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        //    {
        //        cleaned = cleaned.Substring(0, cleaned.Length - "```".Length).TrimEnd();
        //    }

        //    return cleaned;
        //}

        public GenerativeModel CreateSuggestionModel(QuickTools? tools = null)
        {
            var apiKeySuggest = GetAiSettingValue(AISettingType.API_SUGGESTION);
            var modelSuggestion = GetAiSettingValue(AISettingType.MODEL_SUGGESTION);
            var suggestionAiClient = new GoogleAi(apiKeySuggest);

            var suggestClient = suggestionAiClient.CreateGenerativeModel(modelSuggestion);
            if (tools != null)
            {
                suggestClient.AddFunctionTool(tools);
            }
            return suggestClient;
        }

        public async Task<List<long>> ItemCharacteristicSuggestion(string json, string occasion, string usercharacteristic)
        {
            var requestJsonMode = new GenerateContentRequest();
            requestJsonMode.UseJsonMode<List<int>>();

            var systemFormatParts = new List<Part>
            {
                new Part { Text = @"Based on user characteristic and/or occasion, determine what we expected item should have based on provide data. Not creative, **MUST USE PROVIDE DATA** (min 2 id, max 4 ids)" }
            };

            requestJsonMode.SystemInstruction = new Content
            {
                Parts = systemFormatParts,
                Role = "system"
            };

            var userParts = new List<Part>
            {
                new Part { Text = $"User Characteristics: {usercharacteristic}" },
                new Part { Text = $"Occasion: {occasion}" },
                new Part { Text = $"Provide data: {json}" }
            };

            requestJsonMode.AddContent(new Content
            {
                Parts = userParts,
                Role = "user"
            });

            const int maxRetryAttempts = 5;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("ItemCharacteristicSuggestion: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    var result = await _generativeModel.GenerateObjectAsync<List<long>>(requestJsonMode);

                    if (result == null || !result.Any())
                    {
                        _logger.LogWarning("ItemCharacteristicSuggestion: Attempt {Attempt} returned empty result", attempt);

                        if (attempt == maxRetryAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI model returned empty item list");
                        }

                        await Task.Delay(500);
                        continue;
                    }

                    _logger.LogInformation("ItemCharacteristicSuggestion: Successfully returned {Count} ids on attempt {Attempt}", result.Count, attempt);
                    return result;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ItemCharacteristicSuggestion: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: {ex.Message}");
                    }

                    await Task.Delay(500);
                }
            }

            throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
        }

        public async Task<OutfitSelectionModel> ChooseOutfitV2(string json, string occasion, string usercharacteristic, string? weather = null)
        {
            const int maxRetryAttempts = 5;

            // Thêm random delay nhỏ để mỗi request có seed time khác nhau
            var randomDelay = Random.Shared.Next(50, 200);
            await Task.Delay(randomDelay);

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("ChooseOutfitV2: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    // Tạo unique seed cho mỗi request
                    var uniqueSeed = $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}-{Random.Shared.Next(1000, 9999)}";

                    var requestJsonMode = new GenerateContentRequest();
                    requestJsonMode.UseJsonMode<OutfitSelectionModel>();
                    //requestJsonMode.GenerationConfig = new GenerationConfig
                    //{
                    //    MaxOutputTokens = 2000,
                    //    Temperature = 1.3f,  // Tăng lên để tăng tính ngẫu nhiên
                    //    TopP = 0.95f,
                    //    TopK = 50
                    //};

                    var systemParts = new List<Part>
            {
                new Part { Text = $@"[Unique Seed: {uniqueSeed}]
You are an expert fashion stylist AI. Your task is to select a complete outfit from the provided list of items based on user characteristics and occasion.

IMPORTANT RULES:
1. Return ONLY valid JSON in English. Do not include Vietnamese text.
2. Do not output any explanations outside the JSON object.
3. Prioritize selecting items where ""itemType"" is null or ""itemType"" is ""0"". After using those, only then consider items with ""itemType"" = ""1"".
4. Select items that form a COMPLETE outfit (typically **3-5 items** covering: top, bottom, shoes, and optional accessories)
5. **CRITICAL - MAXIMIZE DIVERSITY AND CREATIVITY**: 
   - Generate UNIQUE and UNEXPECTED outfit combinations each time
   - Be CREATIVE and ADVENTUROUS - avoid obvious or safe choices
   - Think like a fashion designer presenting different mood boards
   - Consider mixing unexpected pieces that still work harmoniously
   - Experiment with different aesthetics and fashion statements
6. Ensure selected items complement each other in color, style, and occasion fit
7. Do not modify or translate item IDs - use them exactly as provided
8. Return the response in this exact JSON structure:
{{
    ""itemIds"": [1001, 2005, 3012],
    ""reason"": ""<≤50 words in English explaining the outfit combination highlighting color harmony, style match, weather appropriateness, and occasion fit. Do not mention item IDs.>""
}}" }
            };

                    var userParts = new List<Part>
            {
                new Part { Text = $"[Request ID: {Guid.NewGuid()}] [Timestamp: {DateTime.UtcNow.Ticks}]" },
                new Part { Text = $"User Characteristics: {usercharacteristic}" },
                new Part { Text = $"Occasion: {occasion}" }
            };

                    if (!string.IsNullOrEmpty(weather))
                    {
                        userParts.Add(new Part { Text = $"Weather: {weather}" });
                    }

                    userParts.Add(new Part { Text = $"List of Item:\n{json}" });

                    requestJsonMode.SystemInstruction = new Content
                    {
                        Parts = systemParts,
                        Role = "system"
                    };

                    requestJsonMode.AddContent(new Content
                    {
                        Parts = userParts,
                        Role = "user"
                    });

                    var result = await _suggestionModel.GenerateObjectAsync<OutfitSelectionModel>(requestJsonMode);

                    if (result == null || result.ItemIds == null || !result.ItemIds.Any())
                    {
                        _logger.LogWarning("ChooseOutfitV2: Attempt {Attempt} returned empty result", attempt);

                        if (attempt == maxRetryAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI model returned empty outfit selection");
                        }

                        await Task.Delay(500);
                        continue;
                    }

                    _logger.LogInformation("ChooseOutfitV2: Successfully selected {Count} items on attempt {Attempt}",
                        result.ItemIds.Count, attempt);
                    Console.WriteLine("");
                    Console.Write("Item Selection: ");
                    result.ItemIds.ForEach(id => Console.Write(id + ", "));
                    Console.WriteLine("");
                    return result;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChooseOutfitV2: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: {ex.Message}");
                    }

                    await Task.Delay(500);
                }
            }

            throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
        }
    }
}
