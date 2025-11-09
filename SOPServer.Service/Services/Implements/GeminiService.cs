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

        public async Task<GeminiOutfitGenerationModel?> GenerateOutfitDescription(OutfitGenerationContextModel context)
        {
            var prompt = BuildOutfitGenerationPrompt(context);
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

                    if (result != null && !string.IsNullOrEmpty(result.OutfitType))
                    {
                        _logger.LogInformation("GenerateOutfitDescription: Successfully generated outfit description on attempt {Attempt}", attempt);
                        return result;
                    }

                    _logger.LogWarning("GenerateOutfitDescription: Attempt {Attempt} returned incomplete data", attempt);
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

            throw new BadRequestException("Failed to generate outfit description after multiple attempts");
        }

        public async Task<GeminiFinalSelectionModel?> SelectBestOutfit(OutfitGenerationContextModel context, ShortlistedItemsModel shortlistedItems)
        {
            var prompt = BuildOutfitSelectionPrompt(context, shortlistedItems);
            var generateRequest = new GenerateContentRequest();
            generateRequest.AddText(prompt);
            generateRequest.UseJsonMode<GeminiFinalSelectionModel>();

            const int maxRetryAttempts = 3;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("SelectBestOutfit: Attempt {Attempt} of {MaxAttempts}", attempt, maxRetryAttempts);

                    var result = await _generativeModel.GenerateObjectAsync<GeminiFinalSelectionModel>(generateRequest);

                    if (result != null && result.SelectedOutfit != null && !string.IsNullOrEmpty(result.Explanation))
                    {
                        _logger.LogInformation("SelectBestOutfit: Successfully selected outfit on attempt {Attempt}", attempt);
                        return result;
                    }

                    _logger.LogWarning("SelectBestOutfit: Attempt {Attempt} returned incomplete data", attempt);
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

            throw new BadRequestException("Failed to select best outfit after multiple attempts");
        }

        private string BuildOutfitGenerationPrompt(OutfitGenerationContextModel context)
        {
            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine("You are a professional fashion stylist. Based on the following user characteristics and occasion details, generate an outfit recommendation.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("User Characteristics:");
            promptBuilder.AppendLine($"- Gender: {context.Gender}");
            if (context.Age.HasValue)
                promptBuilder.AppendLine($"- Age: {context.Age}");
            if (!string.IsNullOrEmpty(context.Profession))
                promptBuilder.AppendLine($"- Profession: {context.Profession}");
            if (context.PreferredStyles != null && context.PreferredStyles.Count > 0)
                promptBuilder.AppendLine($"- Preferred Styles: {string.Join(", ", context.PreferredStyles)}");
            if (context.FavoriteColors != null && context.FavoriteColors.Count > 0)
                promptBuilder.AppendLine($"- Favorite Colors: {string.Join(", ", context.FavoriteColors)}");
            if (context.AvoidedColors != null && context.AvoidedColors.Count > 0)
                promptBuilder.AppendLine($"- Colors to Avoid: {string.Join(", ", context.AvoidedColors)}");
            if (!string.IsNullOrEmpty(context.Location))
                promptBuilder.AppendLine($"- Location: {context.Location}");
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Occasion Details:");
            if (!string.IsNullOrEmpty(context.OccasionName))
                promptBuilder.AppendLine($"- Occasion: {context.OccasionName}");
            if (!string.IsNullOrEmpty(context.OccasionDescription))
                promptBuilder.AppendLine($"- Description: {context.OccasionDescription}");
            if (!string.IsNullOrEmpty(context.WeatherSnapshot))
                promptBuilder.AppendLine($"- Weather: {context.WeatherSnapshot}");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Generate an outfit recommendation in JSON format with the following structure:");
            promptBuilder.AppendLine("- OutfitType: Either 'Full-Body' (for dresses, jumpsuits, etc.) or 'Separated' (for top and bottom combinations)");
            promptBuilder.AppendLine("- If Full-Body: include one FullBody item with:");
            promptBuilder.AppendLine("  - Type: specific garment type (e.g., 'Maxi Dress', 'Jumpsuit')");
            promptBuilder.AppendLine("  - ColorPalette: list of suitable colors");
            promptBuilder.AppendLine("  - Style: list of style attributes");
            promptBuilder.AppendLine("  - Occasion: list of suitable occasions");
            promptBuilder.AppendLine("  - Season: list of suitable seasons");
            promptBuilder.AppendLine("  - CategoryId: 41 (Full-Body category)");
            promptBuilder.AppendLine("- If Separated: include both Top and Bottom items, each with:");
            promptBuilder.AppendLine("  - Type: specific garment type");
            promptBuilder.AppendLine("  - ColorPalette: list of suitable colors");
            promptBuilder.AppendLine("  - Style: list of style attributes");
            promptBuilder.AppendLine("  - Occasion: list of suitable occasions");
            promptBuilder.AppendLine("  - Season: list of suitable seasons");
            promptBuilder.AppendLine("  - CategoryId: 1 for Top, 2 for Bottom");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Return only the JSON object without any additional text or explanation.");

            return promptBuilder.ToString();
        }

        private string BuildOutfitSelectionPrompt(OutfitGenerationContextModel context, ShortlistedItemsModel shortlistedItems)
        {
            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine("You are a professional fashion stylist. Based on the user's characteristics, occasion, and the shortlisted wardrobe items, select the best matching outfit.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("User Characteristics:");
            promptBuilder.AppendLine($"- Gender: {context.Gender}");
            if (context.Age.HasValue)
                promptBuilder.AppendLine($"- Age: {context.Age}");
            if (!string.IsNullOrEmpty(context.Profession))
                promptBuilder.AppendLine($"- Profession: {context.Profession}");
            if (context.PreferredStyles != null && context.PreferredStyles.Count > 0)
                promptBuilder.AppendLine($"- Preferred Styles: {string.Join(", ", context.PreferredStyles)}");
            if (context.FavoriteColors != null && context.FavoriteColors.Count > 0)
                promptBuilder.AppendLine($"- Favorite Colors: {string.Join(", ", context.FavoriteColors)}");
            if (!string.IsNullOrEmpty(context.Location))
                promptBuilder.AppendLine($"- Location: {context.Location}");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Occasion Details:");
            if (!string.IsNullOrEmpty(context.OccasionName))
                promptBuilder.AppendLine($"- Occasion: {context.OccasionName}");
            if (!string.IsNullOrEmpty(context.OccasionDescription))
                promptBuilder.AppendLine($"- Description: {context.OccasionDescription}");
            if (!string.IsNullOrEmpty(context.WeatherSnapshot))
                promptBuilder.AppendLine($"- Weather: {context.WeatherSnapshot}");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Shortlisted Items:");

            if (shortlistedItems.FullBodyItems != null && shortlistedItems.FullBodyItems.Count > 0)
            {
                promptBuilder.AppendLine("Full-Body Options:");
                foreach (var item in shortlistedItems.FullBodyItems)
                {
                    promptBuilder.AppendLine($"  - ID: {item.Id}, Name: {item.Name}, Category: {item.CategoryName}");
                    promptBuilder.AppendLine($"    Color: {item.Color}, Description: {item.AiDescription}");
                    if (item.Styles != null && item.Styles.Count > 0)
                        promptBuilder.AppendLine($"    Styles: {string.Join(", ", item.Styles)}");
                    if (item.Occasions != null && item.Occasions.Count > 0)
                        promptBuilder.AppendLine($"    Occasions: {string.Join(", ", item.Occasions)}");
                }
            }

            if (shortlistedItems.TopItems != null && shortlistedItems.TopItems.Count > 0)
            {
                promptBuilder.AppendLine("Top Options:");
                foreach (var item in shortlistedItems.TopItems)
                {
                    promptBuilder.AppendLine($"  - ID: {item.Id}, Name: {item.Name}, Category: {item.CategoryName}");
                    promptBuilder.AppendLine($"    Color: {item.Color}, Description: {item.AiDescription}");
                    if (item.Styles != null && item.Styles.Count > 0)
                        promptBuilder.AppendLine($"    Styles: {string.Join(", ", item.Styles)}");
                    if (item.Occasions != null && item.Occasions.Count > 0)
                        promptBuilder.AppendLine($"    Occasions: {string.Join(", ", item.Occasions)}");
                }
            }

            if (shortlistedItems.BottomItems != null && shortlistedItems.BottomItems.Count > 0)
            {
                promptBuilder.AppendLine("Bottom Options:");
                foreach (var item in shortlistedItems.BottomItems)
                {
                    promptBuilder.AppendLine($"  - ID: {item.Id}, Name: {item.Name}, Category: {item.CategoryName}");
                    promptBuilder.AppendLine($"    Color: {item.Color}, Description: {item.AiDescription}");
                    if (item.Styles != null && item.Styles.Count > 0)
                        promptBuilder.AppendLine($"    Styles: {string.Join(", ", item.Styles)}");
                    if (item.Occasions != null && item.Occasions.Count > 0)
                        promptBuilder.AppendLine($"    Occasions: {string.Join(", ", item.Occasions)}");
                }
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Select the best matching outfit and provide:");
            promptBuilder.AppendLine("- SelectedOutfit: contains either FullBodyId (for full-body outfit) OR both TopId and BottomId (for separated outfit)");
            promptBuilder.AppendLine("- Explanation: A short text (2-3 sentences) explaining why these items were chosen and how they match the user's context");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Return only the JSON object without any additional text.");

            return promptBuilder.ToString();
        }
    }
}
