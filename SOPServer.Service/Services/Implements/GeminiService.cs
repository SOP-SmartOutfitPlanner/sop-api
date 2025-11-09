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

        public async Task<GeminiOutfitGenerationModel?> GenerateOutfitDescription(string userContext)
        {
            var prompt = $@"Based on the following user context, generate a complete outfit recommendation in JSON format.

User Context:
{userContext}

Generate an outfit structure with the following fields:
- outfitType: either 'Separated' (top, bottom, shoes) or 'FullBody' (one-piece outfit)
- description: a brief description of the overall outfit
- items: an array of items, each with:
  - category: 'Top', 'Bottom', 'Shoe', 'Accessory', or 'FullBody'
  - description: detailed description of what this item should look like
- occasion: the occasion this outfit is suitable for
- colorPalette: an array of color names that work well together for this outfit

Important:
- If outfitType is 'FullBody', include only one item with category 'FullBody' (no separate Top/Bottom)
- If outfitType is 'Separated', include Top, Bottom, and Shoe items
- You may include Accessory items for both outfit types
- Consider the user's preferred colors, style, weather, and occasion";

            var generateRequest = new GenerateContentRequest();
            generateRequest.AddText(prompt);
            generateRequest.UseJsonMode<GeminiOutfitGenerationModel>();

            const int maxRetryAttempts = 3;
            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("GenerateOutfitDescription: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);
                    var result = await _generativeModel.GenerateObjectAsync<GeminiOutfitGenerationModel>(generateRequest);
                    _logger.LogInformation("GenerateOutfitDescription: Successfully generated outfit on attempt {Attempt}", attempt);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GenerateOutfitDescription: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);
                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"Failed to generate outfit description: {ex.Message}");
                    }
                }
            }

            throw new BadRequestException("Failed to generate outfit description");
        }

        public async Task<GeminiOutfitSelectionResponse?> SelectBestOutfit(string userContext, List<CategoryItemCandidates> candidates)
        {
            var candidatesText = string.Join("\n", candidates.Select(c =>
                $"Category: {c.Category}\n" +
                string.Join("\n", c.Items.Select(item =>
                    $"  - ID: {item.Id}, Name: {item.Name}, Color: {item.Color}, Pattern: {item.Pattern}, Fabric: {item.Fabric}, Description: {item.Description}"))
            ));

            var prompt = $@"Based on the following user context and available wardrobe items, select the best outfit combination.

User Context:
{userContext}

Available Items by Category:
{candidatesText}

Select the best matching items (one from each category) and provide:
- selectedItemIds: an array of item IDs that form the best outfit
- explanation: a detailed explanation of why these items work well together and match the user's context, style, and occasion

Consider:
- Color harmony and the user's preferred colors
- Style consistency with the user's preferences
- Appropriateness for the weather and occasion
- How well the items complement each other";

            var generateRequest = new GenerateContentRequest();
            generateRequest.AddText(prompt);
            generateRequest.UseJsonMode<GeminiOutfitSelectionResponse>();

            const int maxRetryAttempts = 3;
            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("SelectBestOutfit: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);
                    var result = await _generativeModel.GenerateObjectAsync<GeminiOutfitSelectionResponse>(generateRequest);
                    _logger.LogInformation("SelectBestOutfit: Successfully selected outfit on attempt {Attempt}", attempt);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SelectBestOutfit: Error on attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);
                    if (attempt == maxRetryAttempts)
                    {
                        throw new BadRequestException($"Failed to select best outfit: {ex.Message}");
                    }
                }
            }

            throw new BadRequestException("Failed to select best outfit");
        }
    }
}
