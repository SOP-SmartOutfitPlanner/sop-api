using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.StyleModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using AutoMapper;
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
        private readonly IMapper _mapper;
        private readonly IRedisService _redisService;

        private readonly HashSet<string> _allowedMime = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };
        private static readonly TimeSpan CacheTTL = TimeSpan.FromHours(1);

        private readonly string _promptValidation = @"
You are a fashion image validator. Analyze the given image and return a JSON object that matches the following C# class:

public class ImageValidation
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
}

Rules:

- If the image meets ALL conditions, return:
  {
    ""IsValid"": true,
    ""Message"": ""The image is of good quality""
  }

- If the image does NOT meet the conditions, return:
  {
    ""IsValid"": false,
    ""Message"": ""<Reason why the image is invalid, written in English>""
  }

Conditions for IsValid = true:
  • The primary subject is a single clothing item (shirt, pants, dress, shoes, bag, hat, scarf, sunglasses, jewelry, belt, etc.).
  • The item is clearly visible, centered, and mostly unobstructed.
  • The image is in focus, sharp, and well-lit enough to recognize the item’s details.
  • The background should be reasonably clean and not overly distracting — it can contain some colors or minimal objects, as long as the clothing item remains the main focus.
  • The content must be safe for work (no nudity, no sensitive/explicit material, no violence, no illegal content).

If the image fails, Message must clearly explain why in English, for example:
  • ""No recognizable clothing item detected.""
  • ""The image contains multiple objects or people.""
  • ""The image is blurry or lacks focus.""
  • ""The clothing item is partially obstructed.""
  • ""The image has a watermark or poor lighting.""
  • ""The background is too complex, making it hard to identify the clothing item.""
  • ""The image contains sensitive or inappropriate content.""

Output strictly in JSON format. Do not include explanations, comments, or code blocks.
";


        private readonly string _promptDescription = @"
You are a professional fashion expert. Analyze the clothing item in the provided image and return a valid JSON object that follows the structure below:

{
  ""Color"": ""The main color of the item (e.g., Black, White, Navy Blue...)"",
  ""AiDescription"": ""A detailed yet concise English description of the clothing item (up to 100 words), describing its appearance, style, and possible use occasions."",
  ""WeatherSuitable"": ""The type of weather suitable for wearing this item (e.g., Summer, Cold weather, Rainy, Mild weather...)"",
  ""Condition"": ""The condition of the item (e.g., New, Used, Slightly worn...)"",
  ""Pattern"": ""The visible pattern if any (e.g., Solid, Striped, Plaid, Floral, Logo...)"",
  ""Fabric"": ""The main fabric or material (e.g., Cotton, Silk, Denim, Wool, Leather, Polyester...)""
}

Only return a valid JSON object. Do not include any explanations, comments, or extra text.
";


        public GeminiService(IOptions<GeminiSettings> geminiSettings, IUnitOfWork unitOfWork, IMapper mapper, IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _redisService = redisService;

            var apiKey = GetAISettingValue(AISettingType.API_ITEM_ANALYZING);
            var modelId = GetAISettingValue(AISettingType.MODEL_ANALYZING);

            var googleAi = new GoogleAi(apiKey);
            _generativeModel = googleAi.CreateGenerativeModel(modelId);
        }

        private string GetAISettingValue(AISettingType type)
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
            // Try to get cached data first, fetch from DB in parallel if not cached
            var promptTask = GetOrFetchPromptAsync(AISettingType.DESCRIPTION_ITEM_PROMPT);
            var stylesTask = GetOrFetchStylesAsync();
            var occasionsTask = GetOrFetchOccasionsAsync();
            var seasonsTask = GetOrFetchSeasonsAsync();

            // Wait for all data to be available
            await Task.WhenAll(promptTask, stylesTask, occasionsTask, seasonsTask);

            var descriptionPromptValue = await promptTask;
            var stylesModel = await stylesTask;
            var occasionsModel = await occasionsTask;
            var seasonsModel = await seasonsTask;

            //json rules
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            //mapped to json
            var stylesJson = JsonSerializer.Serialize(stylesModel, jsonOptions);
            var occasionsJson = JsonSerializer.Serialize(occasionsModel, jsonOptions);
            var seasonsJson = JsonSerializer.Serialize(seasonsModel, jsonOptions);

            string finalPrompt = descriptionPromptValue;
            finalPrompt = finalPrompt.Replace("{{styles}}", stylesJson);
            finalPrompt = finalPrompt.Replace("{{occasions}}", occasionsJson);
            finalPrompt = finalPrompt.Replace("{{seasons}}", seasonsJson);

            var request = new GenerateContentRequest();
            request.AddInlineData(base64Image, mimeType);
            request.UseJsonMode<ItemModelAI>();
            request.AddText(finalPrompt);


            return await _generativeModel.GenerateObjectAsync<ItemModelAI>(request);
        }

        public async Task<ImageValidation> ImageValidation(string base64Image, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType) || !_allowedMime.Contains(mimeType))
            {
                throw new NotFoundException(MessageConstants.MIMETYPE_NOT_VALID);
            }

            var validatePromptValue = await GetOrFetchPromptAsync(AISettingType.VALIDATE_ITEM_PROMPT);

            string? rawMessage = string.Empty;

            var request = new GenerateContentRequest();
            request.AddText(validatePromptValue);
            request.AddInlineData(base64Image, mimeType);
            request.UseJsonMode<ImageValidation>();

            return await _generativeModel.GenerateObjectAsync<ImageValidation>(request);
        }

        private async Task<T> GetOrCacheAsync<T>(string cacheKey, Func<Task<T>> fetchFunc, Func<T, bool> isValidFunc)
        {
            var cachedValue = await _redisService.GetAsync<T>(cacheKey);

            if (cachedValue != null && isValidFunc(cachedValue))
            {
                return cachedValue;
            }

            var value = await fetchFunc();

            await _redisService.SetAsync(cacheKey, value, CacheTTL);

            return value;
        }

        private async Task<string> GetOrFetchPromptAsync(AISettingType promptType)
        {
            var cacheKey = RedisKeyConstants.GetAIPromptKey(promptType.ToString());

            return await GetOrCacheAsync(
                cacheKey,
                async () =>
                {
                    var promptSetting = await _unitOfWork.AISettingRepository.GetByTypeAsync(promptType);
                    
                    if (promptSetting == null || string.IsNullOrWhiteSpace(promptSetting.Value))
                    {
                        throw new InvalidOperationException($"AI Setting '{promptType}' not found or has no value configured");
                    }

                    return promptSetting.Value;
                },
                value => !string.IsNullOrEmpty(value)
            );
        }

        private async Task<List<StyleItemModel>> GetOrFetchStylesAsync()
        {
            return await GetOrCacheAsync(
                RedisKeyConstants.AllStylesKey,
                async () =>
                {
                    var styles = await _unitOfWork.StyleRepository.GetAllAsync();
                    return styles.Select(s => new StyleItemModel { Id = s.Id, Name = s.Name }).ToList();
                },
                list => list != null && list.Any()
            );
        }

        private async Task<List<OccasionItemModel>> GetOrFetchOccasionsAsync()
        {
            return await GetOrCacheAsync(
                RedisKeyConstants.AllOccasionsKey,
                async () =>
                {
                    var occasions = await _unitOfWork.OccasionRepository.GetAllAsync();
                    return occasions.Select(o => new OccasionItemModel { Id = o.Id, Name = o.Name }).ToList();
                },
                list => list != null && list.Any()
            );
        }

        private async Task<List<SeasonItemModel>> GetOrFetchSeasonsAsync()
        {
            return await GetOrCacheAsync(
                RedisKeyConstants.AllSeasonsKey,
                async () =>
                {
                    var seasons = await _unitOfWork.SeasonRepository.GetAllAsync();
                    return seasons.Select(s => new SeasonItemModel { Id = s.Id, Name = s.Name }).ToList();
                },
                list => list != null && list.Any()
            );
        }
    }

}
