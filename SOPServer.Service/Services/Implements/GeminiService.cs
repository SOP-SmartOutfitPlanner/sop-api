using AutoMapper;
using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CategoryModels;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.StyleModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class GeminiService : IGeminiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GenerativeModel _generativeModel;
        private readonly EmbeddingModel _embeddingModel;
        private readonly IMapper _mapper;
        private readonly QDrantClientSettings _qdrantClientSettings;
        private readonly HashSet<string> _allowedMime = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };

        public GeminiService(IOptions<GeminiSettings> geminiSettings, IUnitOfWork unitOfWork, IMapper mapper, IOptions<QDrantClientSettings> qdrantClientSettings)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _qdrantClientSettings = qdrantClientSettings.Value;

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

        public async Task<ItemModelAI?> ImageGenerateContent(string base64Image, string mimeType)
        {
            var descriptionPromptSetting = await _unitOfWork.AISettingRepository.GetByTypeAsync(AISettingType.DESCRIPTION_ITEM_PROMPT);

            var styles = await _unitOfWork.StyleRepository.GetAllAsync();
            var occasions = await _unitOfWork.OccasionRepository.GetAllAsync();
            var seasons = await _unitOfWork.SeasonRepository.GetAllAsync();
            var categories = await _unitOfWork.CategoryRepository.GetAllAsync();

            //get and map
            var stylesModel = styles.Where(s => s.CreatedBy == CreatedBy.SYSTEM && !s.IsDeleted).Select(s => new StyleItemModel { Id = s.Id, Name = s.Name });
            var occasionsModel = occasions.Where(o => !o.IsDeleted).Select(o => new OccasionItemModel { Id = o.Id, Name = o.Name });
            var seasonsModel = seasons.Where(s => !s.IsDeleted).Select(s => new SeasonItemModel { Id = s.Id, Name = s.Name });
            var categoryModel = categories.Where(x => x.ParentId != null && !x.IsDeleted).Select(c => new CategoryItemModel { Id = c.Id, Name = c.Name });

            // JSON serializer options
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            //mapped to json
            var stylesJson = JsonSerializer.Serialize(stylesModel, serializerOptions);
            var occasionsJson = JsonSerializer.Serialize(occasionsModel, serializerOptions);
            var seasonsJson = JsonSerializer.Serialize(seasonsModel, serializerOptions);
            var categoriesJson = JsonSerializer.Serialize(categoryModel, serializerOptions);

            var promptText = descriptionPromptSetting.Value;
            promptText = promptText.Replace("{{styles}}", stylesJson);
            promptText = promptText.Replace("{{occasions}}", occasionsJson);
            promptText = promptText.Replace("{{seasons}}", seasonsJson);
            promptText = promptText.Replace("{{categories}}", categoriesJson);

            var generateRequest = new GenerateContentRequest();
            generateRequest.AddInlineData(base64Image, mimeType);
            generateRequest.UseJsonMode<ItemModelAI>();
            generateRequest.AddText(promptText);

            return await _generativeModel.GenerateObjectAsync<ItemModelAI>(generateRequest);
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

            return await _generativeModel.GenerateObjectAsync<ImageValidation>(generateRequest);
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
    }
}
