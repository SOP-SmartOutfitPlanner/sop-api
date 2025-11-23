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
            var modelIdSuggestiong = GetAiSettingValue(AISettingType.MODEL_ANALYZING);
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

        public async Task<OutfitSelectionModel> ChooseOutfit(long userId, string occasion, string usercharacteristic, List<QDrantSearchModels> items, string? weather = null)
        {
            var choosePromptSetting = await _unitOfWork.AISettingRepository.GetByTypeAsync(AISettingType.OUTFIT_CHOOSE_PROMPT);

            // Convert items to JSON for the AI to understand
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var systemParts = new List<Part>
            {
                new Part { Text = @"You are an expert fashion stylist AI with function-calling capability.

INPUTS: occasion, userCharacteristic

CORE RULES:

1. OCCASION PRIORITY
   - Null/vague occasion = comfortable home wear (loungewear, casual)
   - Otherwise = match formality, activity type, weather
   - Occasion overrides all user preferences

2. USER PREFERENCES
   - Apply only when they don't conflict with occasion

FUNCTION CALLING (MINIMIZE CALLS):

STEP 1: Call SearchItemsByUserIdCompact
   - Evaluate user's wardrobe against occasion and preferences

STEP 2: Call SearchSimilarityItemSystemCompact ONCE ONLY if user items are insufficient:
   - Missing core clothing (tops/bottoms/dresses)
   - Missing necessary accessories (shoes/bags)
   - Items don't match occasion or violate preferences
   - Make ONE call with all missing item types to minimize function calls

OUTFIT REQUIREMENTS:

MUST select exactly 3-5 items total (never less, never more)
- Prioritize user's items first
- Add system items only to fill gaps or reach 3-5 items
- Ensure: color harmony, style consistency, occasion match, climate fit
- Use only valid item IDs from function responses

**OUTPUT**:

1. **MUST INCLUDE Item IDs** (3-5 items) (if system items used, include their IDs). **NO EXPLANATION HERE**.
2. Brief **total explanation** in English (1-2 sentences, max 50 words)" }
            };

            var userParts = new List<Part>
            {
                new Part { Text = $"User ID: {userId}" },
                new Part { Text = $"User Characteristics: {usercharacteristic}" },
            };

            if (!string.IsNullOrEmpty(occasion))
            {
                userParts.Add(new Part { Text = $"Occasion: {occasion}" });
            } else userParts.Add(new Part { Text = $"Occasion: null" });

            if (!string.IsNullOrEmpty(weather))
            {
                userParts.Add(new Part { Text = $"Weather: {weather}" });
            }

            // Step 1: Generate outfit response
            var outfitResponse = await GenerateOutfitResponseAsync(systemParts, userParts);

            // Step 2: Normalize to JSON
            var result = await NormalizeOutfitResponseToJsonAsync(outfitResponse);

            return result;
        }

        private async Task<string> GenerateOutfitResponseAsync(List<Part> systemParts, List<Part> userParts)
        {
            const int maxOutfitAttempts = 5;

            for (int attempt = 1; attempt <= maxOutfitAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("ChooseOutfit - Outfit Generation: Attempt {Attempt} of {MaxAttempts}", attempt, maxOutfitAttempts);

                    // Create tools with both SearchSimilarityItemSystem and SearchItemIdsByUserId
                    QuickTools tools = new QuickTools([
                        _qdrantService.Value.SearchItemsByUserIdCompact,
                        _qdrantService.Value.SearchSimilarityItemSystemCompact
                    ]);
                    var model = CreateSuggestionModel(tools);

                    var generateRequest = new GenerateContentRequest();
                    generateRequest.GenerationConfig = new GenerationConfig
                    {
                        Temperature = 0.5f,
                        MaxOutputTokens = 1000
                    };
                    generateRequest.SystemInstruction = new Content
                    {
                        Parts = systemParts,
                        Role = "system"
                    };

                    var userContent = new Content { Parts = userParts, Role = "user" };
                    generateRequest.AddContent(userContent);

                    var response = await model.GenerateContentAsync(generateRequest);
                    var outfitResponse = response.Text;

                    if (string.IsNullOrWhiteSpace(outfitResponse))
                    {
                        _logger.LogWarning("ChooseOutfit - Outfit Generation: Attempt {Attempt} returned empty response", attempt);

                        if (attempt == maxOutfitAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI model returned empty outfit response");
                        }

                        await Task.Delay(500);
                        continue;
                    }

                    _logger.LogInformation("ChooseOutfit - Outfit Generation: Successfully generated on attempt {Attempt}", attempt);
                    Console.WriteLine("OUTFIT RESPONSE: " + outfitResponse);
                    return outfitResponse;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChooseOutfit - Outfit Generation: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxOutfitAttempts);

                    if (attempt == maxOutfitAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: {ex.Message}");
                    }

                    await Task.Delay(1000);
                }
            }

            throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
        }

        private async Task<OutfitSelectionModel> NormalizeOutfitResponseToJsonAsync(string outfitResponse)
        {
            const int maxJsonAttempts = 5;

            for (int attempt = 1; attempt <= maxJsonAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("ChooseOutfit - JSON Normalization: Attempt {Attempt} of {MaxAttempts}", attempt, maxJsonAttempts);

                    var requestJsonMode = new GenerateContentRequest();
                    requestJsonMode.UseJsonMode<OutfitSelectionModel>();

                    var systemFormatParts = new List<Part>
                    {
                        new Part { Text = @"Format a response to json mode for me { ""itemIds"": [12, 34], ""reason"": ""≤50 words about color harmony, style match, occasion fit."" }" }
                    };

                    var aiResponsePart = new List<Part>
                    {
                        new Part { Text = outfitResponse },
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
                        _logger.LogWarning("ChooseOutfit - JSON Normalization: Attempt {Attempt} returned empty result", attempt);

                        if (attempt == maxJsonAttempts)
                        {
                            throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI model returned empty outfit selection after JSON normalization");
                        }

                        await Task.Delay(500);
                        continue;
                    }

                    _logger.LogInformation("ChooseOutfit - JSON Normalization: Successfully normalized with {Count} items on attempt {Attempt}", 
                        result.ItemIds.Count, attempt);

                    return result;
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChooseOutfit - JSON Normalization: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxJsonAttempts);

                    if (attempt == maxJsonAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: Failed to normalize response - {ex.Message}");
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
    }
}
