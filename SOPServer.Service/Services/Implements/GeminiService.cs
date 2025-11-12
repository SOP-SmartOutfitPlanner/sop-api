using AutoMapper;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SOPServer.Service.Services.Implements
{
    public class GeminiService : IGeminiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GenerativeModel _generativeModel;
        private readonly EmbeddingModel _embeddingModel;
        private readonly IMapper _mapper;
        private readonly QDrantClientSettings _qdrantClientSettings;
        private readonly ILogger<GeminiService> _logger;
        private readonly HashSet<string> _allowedMime = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };

        public GeminiService(IOptions<GeminiSettings> geminiSettings, IUnitOfWork unitOfWork, IMapper mapper, IOptions<QDrantClientSettings> qdrantClientSettings, ILogger<GeminiService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<List<string>?> GenerateOutfitSuggestItem(string userCharacteristic, string? occasion)
        {
            var generateRequest = new GenerateContentRequest();
            generateRequest.UseJsonMode<List<string>>();
            var occasionPart = !string.IsNullOrWhiteSpace(occasion)
                ? $"Occasion: {occasion}"
                : string.Empty;

            var prompt = $@"
                You are a fashion stylist.

                Create a cohesive outfit as a list of clothing and accessories (no fixed slots, layering allowed).
                Each item = short vivid description with type, color, material, and style.

                Context:
                User: {userCharacteristic}
                {occasionPart}

                Rules:
                - Items must match in color, texture, and style.
                - Fit the occasion or user preference.
                - Return ONLY a JSON array of strings.

                Example:
                [
                  ""White linen shirt with relaxed fit"",
                  ""Beige overshirt with wooden buttons"",
                  ""Light blue jeans"",
                  ""White sneakers"",
                  ""Brown leather watch""
                ]
            ";

            generateRequest.AddText(prompt);
            return await _generativeModel.GenerateObjectAsync<List<string>>(generateRequest);
        }

        public async Task<OutfitSelectionModel> SelectOutfitFromItems(string userCharacteristic, string occasion, List<ItemModel> availableItems)
        {
            var generateRequest = new GenerateContentRequest();
            generateRequest.UseJsonMode<OutfitSelectionModel>();

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Convert items to structured format for AI
            var itemsJson = JsonSerializer.Serialize(availableItems.Select(item => new
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.CategoryName,
                Color = item.Color,
                Brand = item.Brand,
                Description = !string.IsNullOrWhiteSpace(item.AiDescription) ? item.AiDescription : item.Name,
                Fabric = item.Fabric,
                Pattern = item.Pattern,
                Condition = item.Condition,
                WeatherSuitable = item.WeatherSuitable,
                Styles = item.Styles?.Select(s => s.Name).ToList() ?? new List<string>(),
                Occasions = item.Occasions?.Select(o => o.Name).ToList() ?? new List<string>(),
                Seasons = item.Seasons?.Select(s => s.Name).ToList() ?? new List<string>()
            }), serializerOptions);

            var occasionPart = !string.IsNullOrWhiteSpace(occasion)
                ? $"- Occasion context: {occasion}"
                : string.Empty;

            var prompt = $@"
You are an expert fashion stylist. Your task is to select a cohesive outfit from the available wardrobe items.

User Context:
- User characteristics: {userCharacteristic}
{occasionPart}

Available items in the wardrobe (JSON format):
{itemsJson}

Instructions:
1. Select items that create a complete, stylish, and cohesive outfit
2. Consider color harmony, style consistency, and appropriateness for the occasion
3. Include essential pieces: top, bottom (or dress), footwear
4. Add complementary accessories if available
5. Ensure items work well together in terms of formality, style, and weather suitability
6. You can select ANY NUMBER of items that work together - no minimum or maximum limit
7. Focus on creating a harmonious, wearable outfit

Return your selection as JSON with:
- ""itemIds"": array of selected item IDs (as numbers)
- ""reason"": brief explanation (2-3 sentences) of why these items work together, focusing on color harmony, style coherence, and suitability for the occasion

Example response format:
{{
  ""itemIds"": [1, 5, 12, 23],
  ""reason"": ""This outfit combines a crisp white shirt with navy chinos for a smart-casual look perfect for your office environment. The brown leather loafers add sophistication while the minimalist watch provides a polished finishing touch. The color palette is cohesive and professional.""
}}

Be selective but creative. Choose items that genuinely complement each other.";

            generateRequest.AddText(prompt);

            const int maxRetryAttempts = 3;
            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("SelectOutfitFromItems: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);
                    var result = await _generativeModel.GenerateObjectAsync<OutfitSelectionModel>(generateRequest);

                    if (result != null && result.ItemIds != null && result.ItemIds.Any() && !string.IsNullOrWhiteSpace(result.Reason))
                    {
                        _logger.LogInformation("SelectOutfitFromItems: Successfully selected outfit on attempt {Attempt}", attempt);
                        return result;
                    }

                    _logger.LogWarning("SelectOutfitFromItems: Attempt {Attempt} returned incomplete data", attempt);

                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"{MessageConstants.OUTFIT_SUGGESTION_FAILED}: AI returned incomplete outfit selection");
                    }
                }
                catch (BadRequestException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SelectOutfitFromItems: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

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
