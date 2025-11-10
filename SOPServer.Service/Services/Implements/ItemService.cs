using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CategoryModels;
using SOPServer.Service.BusinessModels.FirebaseModels;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.RemBgModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.StyleModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.Utils;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SOPServer.Service.Services.Implements
{
    public class ItemService : IItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IGeminiService _geminiService;
        private readonly IMinioService _minioService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IQdrantService _qdrantService;

        public ItemService(IUnitOfWork unitOfWork, IMapper mapper, IGeminiService geminiService, IMinioService minioService, IHttpClientFactory httpClientFactory, IQdrantService qdrantService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _geminiService = geminiService;
            _minioService = minioService;
            _httpClientFactory = httpClientFactory;
            _qdrantService = qdrantService;
        }

        public async Task<BaseResponseModel> UpdateItemAsync(long id, ItemCreateModel model)
        {
            var entity = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(id,
                    include: query => query
               .Include(x => x.ItemOccasions)
         .Include(x => x.ItemSeasons)
            .Include(x => x.ItemStyles));

            if (entity == null)
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);

            //TODO validate exsited occasion, season, style, category
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(model.CategoryId);
            if (category == null)
            {
                throw new NotFoundException(MessageConstants.CATEGORY_NOT_EXIST);
            }
            if(category.ParentId == null)
            {
                throw new BadRequestException(MessageConstants.CATEGORY_PARENT_NOT_EXIST);
            }

            foreach(var styleId in model.StyleIds)
            {
                var style =  await _unitOfWork.StyleRepository.GetByIdAsync(styleId);
                if(style == null)
                {
                    throw new NotFoundException($"Style with id {styleId} does not exist");
                }
            }

            foreach(var occasionId in model.OccasionIds)
            {
                var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(occasionId);
                if (occasion == null)
                {
                    throw new NotFoundException($"Occasion with id {occasionId} does not exist");
                }
            }

            foreach(var seasonId in model.SeasonIds)
            {
                var season = await _unitOfWork.SeasonRepository.GetByIdAsync(seasonId);
                if (season == null)
                {
                    throw new NotFoundException($"Season with id {seasonId} does not exist");
                }
            }

            _mapper.Map(model, entity);
            _unitOfWork.ItemRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            // Update relationships if provided
            await UpdateItemRelationshipsAsync(id, model.StyleIds, model.OccasionIds, model.SeasonIds);

            var updatedItem = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(id,
   include: query => query
   .Include(x => x.Category).ThenInclude(x => x.Parent)
        .Include(x => x.User)
   .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
      .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
          .Include(x => x.ItemStyles).ThenInclude(x => x.Style));

            var stringItem = ConvertItemToEmbeddingString(updatedItem);

            var embeddingText = await _geminiService.EmbeddingText(stringItem);

            if (embeddingText != null && embeddingText.Any())
            {
                // Prepare payload for Qdrant
                var payload = new Dictionary<string, object>
                {
                    { "UserId", updatedItem.UserId ?? 0 },
                    { "Name", updatedItem.Name ?? "" },
                    { "Category", updatedItem.Category?.Parent.Name ?? "" },
                    { "Color", updatedItem.Color ?? "" },
                    { "Brand", updatedItem.Brand ?? "" }
                };

                // Upload to Qdrant
                await _qdrantService.UpSertItem(embeddingText, payload, updatedItem.Id);
            }

            var data = _mapper.Map<ItemModel>(updatedItem);
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_UPDATE_SUCCESS,
                Data = data
            };
        }

        public async Task<BaseResponseModel> AddNewItem(ItemCreateModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var category = await _unitOfWork.CategoryRepository.GetByIdIncludeAsync(model.CategoryId, include: x => x.Include(x => x.Parent));
            if (category == null)
            {
                throw new NotFoundException(MessageConstants.CATEGORY_NOT_EXIST);
            }

            var newItem = _mapper.Map<Item>(model);

            await _unitOfWork.ItemRepository.AddAsync(newItem);
            await _unitOfWork.SaveAsync();

            // Add relationships if provided
            await AddItemRelationshipsAsync(newItem.Id, model.StyleIds, model.OccasionIds, model.SeasonIds);

            var newItemInclude = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(newItem.Id,
                                include: query => query
                                        .Include(x => x.Category).ThenInclude(c => c.Parent)
                                        .Include(x => x.User)
                                        .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                                        .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                                        .Include(x => x.ItemStyles).ThenInclude(x => x.Style));

            // Convert item to structured string for embedding
            var stringItem = ConvertItemToEmbeddingString(newItemInclude);

            var embeddingText = await _geminiService.EmbeddingText(stringItem);

            if (embeddingText != null && embeddingText.Any())
            {
                // Prepare payload for Qdrant
                var payload = new Dictionary<string, object>
                {
                    { "UserId", newItemInclude.UserId ?? 0 },
                    { "Name", newItemInclude.Name ?? "" },
                    { "Category", newItemInclude.Category?.Parent.Name ?? "" },
                    { "Color", newItemInclude.Color ?? "" },
                    { "Brand", newItemInclude.Brand ?? "" }
                };

                // Upload to Qdrant
                await _qdrantService.UpSertItem(embeddingText, payload, newItemInclude.Id);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_CREATE_SUCCESS,
                Data = _mapper.Map<ItemModel>(newItemInclude)
            };
        }

        public async Task<BaseResponseModel> DeleteItemByIdAsync(long id)
        {
            var item = await _unitOfWork.ItemRepository.GetByIdAsync(id);

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            _unitOfWork.ItemRepository.SoftDeleteAsync(item);
            await _unitOfWork.SaveAsync();

            await _qdrantService.DeleteItem(id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DELETE_ITEM_SUCCESS
            };
        }

        public async Task<BaseResponseModel> GetItemById(long id)
        {
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(id,
 include: query => query.Include(x => x.Category)
    .Include(x => x.User)
       .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
 .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
        .Include(x => x.ItemStyles).ThenInclude(x => x.Style));
            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_GET_SUCCESS,
                Data = _mapper.Map<ItemModel>(item)
            };
        }

        public async Task<BaseResponseModel> GetItemByUserPaginationAsync(PaginationParameter paginationParameter, long userid, ItemFilterModel filter)
        {
            if (filter == null)
            {
                throw new BadRequestException("Filter is required");
            }

            var items = await _unitOfWork.ItemRepository.ToPaginationIncludeAsync(paginationParameter,
                                    include: query => query
          .Include(x => x.Category)
            .Include(x => x.User)
     .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
     .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
        .Include(x => x.ItemStyles).ThenInclude(x => x.Style),
            filter: x => x.UserId == userid
               && (!filter.IsAnalyzed.HasValue || x.IsAnalyzed == filter.IsAnalyzed.Value)
    && (!filter.CategoryId.HasValue || x.CategoryId == filter.CategoryId.Value)
   && (!filter.StyleId.HasValue || x.ItemStyles.Any(isr => !isr.IsDeleted && isr.StyleId == filter.StyleId.Value))
                 && (!filter.SeasonId.HasValue || x.ItemSeasons.Any(ss => !ss.IsDeleted && ss.SeasonId == filter.SeasonId.Value))
     && (!filter.OccasionId.HasValue || x.ItemOccasions.Any(ss => !ss.IsDeleted && ss.OccasionId == filter.OccasionId.Value))
  && (string.IsNullOrEmpty(paginationParameter.Search) || x.Name.Contains(paginationParameter.Search)),
   orderBy: x => filter.SortByDate.HasValue && filter.SortByDate.Value == Repository.Enums.SortOrder.Ascending
          ? x.OrderBy(item => item.CreatedDate)
    : x.OrderByDescending(item => item.CreatedDate));

            var itemModels = _mapper.Map<Pagination<ItemModel>>(items);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_ITEM_SUCCESS,
                Data = new ModelPaging
                {
                    Data = itemModels,
                    MetaData = new
                    {
                        itemModels.TotalCount,
                        itemModels.PageSize,
                        itemModels.CurrentPage,
                        itemModels.TotalPages,
                        itemModels.HasNext,
                        itemModels.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetItemPaginationAsync(PaginationParameter paginationParameter)
        {
            var items = await _unitOfWork.ItemRepository.ToPaginationIncludeAsync(paginationParameter,
                  include: query => query.Include(x => x.Category)
         .Include(x => x.User)
           .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                 .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
             .Include(x => x.ItemStyles).ThenInclude(x => x.Style),
              orderBy: x => x.OrderByDescending(x => x.CreatedDate));

            var itemModels = _mapper.Map<Pagination<ItemModel>>(items);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_ITEM_SUCCESS,
                Data = new ModelPaging
                {
                    Data = itemModels,
                    MetaData = new
                    {
                        itemModels.TotalCount,
                        itemModels.PageSize,
                        itemModels.CurrentPage,
                        itemModels.TotalPages,
                        itemModels.HasNext,
                        itemModels.HasPrevious
                    }
                }

            };
        }

        //public async Task<BaseResponseModel> GetAnalyzeItem(IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //        throw new BadRequestException(MessageConstants.IMAGE_IS_NOT_VALID);

        //    // 1️⃣ Khởi tạo task song song
        //    var convertBase64Task = ImageUtils.ConvertToBase64Async(file);
        //    var uploadOriginalTask = UploadFileToMinio(file);

        //    // 2️⃣ Chờ cả 2 cùng xong
        //    await Task.WhenAll(convertBase64Task, uploadOriginalTask);

        //    var base64Image = await convertBase64Task;
        //    var uploadToMinio = await uploadOriginalTask;

        //    // 3️⃣ Validation image
        //    //var validation = await _geminiService.ImageValidation(base64Image, file.ContentType);
        //    //if (!validation.IsValid)
        //    //    throw new BadRequestException(validation.Message);

        //    // 4️⃣ Remove background (ph phụ thuộc uploadToMinio)
        //    var fileRemoveBackground = await CallRembgAndGetRemovedFile(
        //        uploadToMinio.DownloadUrl,
        //        uploadToMinio.FileName
        //    );

        //    // 5️⃣ Song song 2 tác vụ sau khi đã có file remove background:
        //    //     - Convert base64 cho file remove background
        //    //     - Upload file remove background lên MinIO
        //    var convertRemovedBase64Task = ImageUtils.ConvertToBase64Async(fileRemoveBackground);
        //    var uploadRemovedFileTask = UploadFileToMinio(fileRemoveBackground);

        //    await Task.WhenAll(convertRemovedBase64Task, uploadRemovedFileTask);

        //    var base64Removed = await convertRemovedBase64Task;
        //    var imageRemBgResponse = await uploadRemovedFileTask;

        //    // 6️⃣ Gọi Gemini summary (chờ base64Removed)
        //    var summaryFromGemini = await _geminiService.ImageGenerateContent(
        //        base64Removed, fileRemoveBackground.ContentType
        //    );

        //    // 7️⃣ Map và trả response
        //    var itemSummary = _mapper.Map<ItemSummaryModel>(summaryFromGemini);
        //    itemSummary.ImageRemBgURL = imageRemBgResponse.DownloadUrl;

        //    return new BaseResponseModel
        //    {
        //        StatusCode = StatusCodes.Status200OK,
        //        Message = MessageConstants.GET_SUMMARY_IMAGE_SUCCESS,
        //        Data = itemSummary
        //    };
        //}

        // Helper: upload file to firebase and return ImageUploadResult
        private async Task<ImageUploadResult> UploadFileToMinio(IFormFile file)
        {
            var uploadResult = await _minioService.UploadImageAsync(file);
            if (uploadResult?.Data is not ImageUploadResult uploadData || string.IsNullOrEmpty(uploadData.DownloadUrl))
            {
                throw new BadRequestException(MessageConstants.FILE_NOT_FOUND);
            }

            return uploadData;
        }

        // Helper: call rembg service and return IFormFile of removed background image
        private async Task<IFormFile> CallRembgAndGetRemovedFile(string imageUrl, string originalFileName)
        {
            var client = _httpClientFactory.CreateClient("RembgClient");

            var requestBody = new RembgRequest
            {
                Input = new RembgInput { Image = imageUrl }
            };

            var responseRemBg = await client.PostAsJsonAsync("predictions", requestBody);

            // delete original uploaded image regardless of rembg result
            await _minioService.DeleteImageAsync(originalFileName);

            if (!responseRemBg.IsSuccessStatusCode)
            {
                throw new BadRequestException(MessageConstants.CALL_REM_BACKGROUND_FAIL);
            }

            var result = await responseRemBg.Content.ReadFromJsonAsync<RembgResponse>();

            if (result == null || !string.Equals(result.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(MessageConstants.REM_BACKGROUND_IMAGE_FAIL);
            }

            var fileRemoveBackground = ImageUtils.Base64ToFormFile(result.Output, originalFileName);
            return fileRemoveBackground;

        }

        // Helper method to add relationships during item creation
        private async Task AddItemRelationshipsAsync(long itemId, List<long> styleIds, List<long> occasionIds, List<long> seasonIds)
        {
            // Add styles
            if (styleIds != null && styleIds.Any())
            {
                foreach (var styleId in styleIds)
                {
                    var style = await _unitOfWork.StyleRepository.GetByIdAsync(styleId);
                    if (style == null)
                    {
                        throw new NotFoundException($"{MessageConstants.STYLE_NOT_EXIST}: {styleId}");
                    }

                    var itemStyle = new ItemStyle
                    {
                        ItemId = itemId,
                        StyleId = styleId
                    };
                    await _unitOfWork.ItemStyleRepository.AddAsync(itemStyle);
                }
            }

            // Add occasions
            if (occasionIds != null && occasionIds.Any())
            {
                foreach (var occasionId in occasionIds)
                {
                    var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(occasionId);
                    if (occasion == null)
                    {
                        throw new NotFoundException($"{MessageConstants.OCCASION_NOT_EXIST}: {occasionId}");
                    }

                    var itemOccasion = new ItemOccasion
                    {
                        ItemId = itemId,
                        OccasionId = occasionId
                    };
                    await _unitOfWork.ItemOccasionRepository.AddAsync(itemOccasion);
                }
            }

            // Add seasons
            if (seasonIds != null && seasonIds.Any())
            {
                foreach (var seasonId in seasonIds)
                {
                    var season = await _unitOfWork.SeasonRepository.GetByIdAsync(seasonId);
                    if (season == null)
                    {
                        throw new NotFoundException($"{MessageConstants.SEASON_NOT_EXIST}: {seasonId}");
                    }

                    var itemSeason = new ItemSeason
                    {
                        ItemId = itemId,
                        SeasonId = seasonId
                    };
                    await _unitOfWork.ItemSeasonRepository.AddAsync(itemSeason);
                }
            }

            if ((styleIds != null && styleIds.Any()) || (occasionIds != null && occasionIds.Any()) || (seasonIds != null && seasonIds.Any()))
            {
                await _unitOfWork.SaveAsync();
            }
        }

        // Helper method to update relationships during item update
        private async Task UpdateItemRelationshipsAsync(long itemId, List<long> styleIds, List<long> occasionIds, List<long> seasonIds)
        {
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(itemId,
                            include: query => query
                                .Include(x => x.ItemStyles)
                                .Include(x => x.ItemOccasions)
                                .Include(x => x.ItemSeasons));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Update styles if provided
            if (styleIds != null)
            {
                // Soft delete existing styles
                var existingItemStyles = item.ItemStyles.Where(ist => !ist.IsDeleted).ToList();
                foreach (var existingItemStyle in existingItemStyles)
                {
                    _unitOfWork.ItemStyleRepository.SoftDeleteAsync(existingItemStyle);
                }

                // Add new styles
                foreach (var styleId in styleIds)
                {
                    var style = await _unitOfWork.StyleRepository.GetByIdAsync(styleId);
                    if (style == null)
                    {
                        throw new NotFoundException($"{MessageConstants.STYLE_NOT_EXIST}: {styleId}");
                    }

                    var itemStyle = new ItemStyle
                    {
                        ItemId = itemId,
                        StyleId = styleId
                    };
                    await _unitOfWork.ItemStyleRepository.AddAsync(itemStyle);
                }
            }

            // Update occasions if provided
            if (occasionIds != null)
            {
                // Soft delete existing occasions
                var existingItemOccasions = item.ItemOccasions.Where(io => !io.IsDeleted).ToList();
                foreach (var existingItemOccasion in existingItemOccasions)
                {
                    _unitOfWork.ItemOccasionRepository.SoftDeleteAsync(existingItemOccasion);
                }

                // Add new occasions
                foreach (var occasionId in occasionIds)
                {
                    var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(occasionId);
                    if (occasion == null)
                    {
                        throw new NotFoundException($"{MessageConstants.OCCASION_NOT_EXIST}: {occasionId}");
                    }

                    var itemOccasion = new ItemOccasion
                    {
                        ItemId = itemId,
                        OccasionId = occasionId
                    };
                    await _unitOfWork.ItemOccasionRepository.AddAsync(itemOccasion);
                }
            }

            // Update seasons if provided
            if (seasonIds != null)
            {
                // Soft delete existing seasons
                var existingItemSeasons = item.ItemSeasons.Where(itemSeason => !itemSeason.IsDeleted).ToList();
                foreach (var existingItemSeason in existingItemSeasons)
                {
                    _unitOfWork.ItemSeasonRepository.SoftDeleteAsync(existingItemSeason);
                }

                // Add new seasons
                foreach (var seasonId in seasonIds)
                {
                    var season = await _unitOfWork.SeasonRepository.GetByIdAsync(seasonId);
                    if (season == null)
                    {
                        throw new NotFoundException($"{MessageConstants.SEASON_NOT_EXIST}: {seasonId}");
                    }

                    var itemSeason = new ItemSeason
                    {
                        ItemId = itemId,
                        SeasonId = seasonId
                    };
                    await _unitOfWork.ItemSeasonRepository.AddAsync(itemSeason);
                }
            }

            if ((styleIds != null && styleIds.Any()) || (occasionIds != null && occasionIds.Any()) || (seasonIds != null && seasonIds.Any()))
            {
                await _unitOfWork.SaveAsync();
            }
        }

        // Helper method to convert Item to structured string for embedding
        private string ConvertItemToEmbeddingString(Item item)
        {
            var sb = new StringBuilder();

            // Basic information
            sb.AppendLine($"Item Name: {item.Name ?? "Unknown"}");

            // Category with hierarchy
            if (item.Category != null)
            {
                var categoryPath = item.Category.Parent != null
                     ? $"{item.Category.Parent.Name} > {item.Category.Name}"
                   : item.Category.Name;
                sb.AppendLine($"Category: {categoryPath}");
            }

            // Physical attributes
            if (!string.IsNullOrEmpty(item.Color))
                sb.AppendLine($"Color: {item.Color}");

            if (!string.IsNullOrEmpty(item.Pattern))
                sb.AppendLine($"Pattern: {item.Pattern}");

            if (!string.IsNullOrEmpty(item.Fabric))
                sb.AppendLine($"Fabric: {item.Fabric}");

            if (!string.IsNullOrEmpty(item.Brand))
                sb.AppendLine($"Brand: {item.Brand}");

            // Condition and weather
            if (!string.IsNullOrEmpty(item.Condition))
                sb.AppendLine($"Condition: {item.Condition}");

            if (!string.IsNullOrEmpty(item.WeatherSuitable))
                sb.AppendLine($"Weather Suitable: {item.WeatherSuitable}");

            // AI Description (important for semantic search)
            if (!string.IsNullOrEmpty(item.AiDescription))
                sb.AppendLine($"Description: {item.AiDescription}");

            // Styles
            var styles = item.ItemStyles?
                .Where(ist => !ist.IsDeleted && ist.Style != null)
       .Select(ist => ist.Style.Name)
                .ToList();
            if (styles != null && styles.Any())
                sb.AppendLine($"Styles: {string.Join(", ", styles)}");

            // Occasions
            var occasions = item.ItemOccasions?
        .Where(io => !io.IsDeleted && io.Occasion != null)
         .Select(io => io.Occasion.Name)
      .ToList();
            if (occasions != null && occasions.Any())
                sb.AppendLine($"Occasions: {string.Join(", ", occasions)}");

            // Seasons
            var seasons = item.ItemSeasons?
     .Where(ise => !ise.IsDeleted && ise.Season != null)
         .Select(ise => ise.Season.Name)
   .ToList();
            if (seasons != null && seasons.Any())
                sb.AppendLine($"Seasons: {string.Join(", ", seasons)}");

            return sb.ToString().Trim();
        }

        public async Task<BaseResponseModel> BulkCreateItemAuto(BulkItemRequestAutoModel bulkUploadModel)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(bulkUploadModel.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var client = _httpClientFactory.CreateClient("AnalysisClient");
            var invalidCategoryItems = new List<object>();
            var newItems = new List<Item>();

            var categories = await _unitOfWork.CategoryRepository.GetAllChildrenCategory();

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var categoriesJson = JsonSerializer.Serialize(_mapper.Map<List<CategoryItemModel>>(categories), serializerOptions);

            var categoryItemPrompt = await _unitOfWork.AISettingRepository.GetByTypeAsync(AISettingType.CATEGORY_ITEM_ANALYSIS_PROMPT);

            string finalPrompt = categoryItemPrompt.Value.Replace("{{categories}}", categoriesJson);
            // Process all images in parallel
            var tasks = bulkUploadModel.ImageURLs.Select(async imageUrl =>
            {
                try
                {
                    var (base64Image, mimeType) = await ImageUtils.ConvertToBase64Async(imageUrl, client);
                    CategoryItemAnalysisModel? categoryAnalysis;

                    // Retry logic for category analysis
                    while (true)
                    {
                        try
                        {
                            categoryAnalysis = await _geminiService.AnalyzingCategory(base64Image, mimeType, finalPrompt);
                            break;
                        }
                        catch (Exception)
                        {
                            // Continue retrying
                        }
                    }

                    if (categoryAnalysis.CategoryId == 0)
                    {
                        return (imageUrl, null); // Invalid category
                    }

                    var newItem = new Item
                    {
                        Name = "Sop Item",
                        CategoryId = categoryAnalysis.CategoryId,
                        UserId = bulkUploadModel.UserId,
                        ImgUrl = imageUrl,
                        IsAnalyzed = false,
                        AIAnalyzeJson = JsonSerializer.Serialize(categoryAnalysis, serializerOptions)
                    };

                    return (imageUrl, newItem);
                }
                catch (Exception)
                {
                    // Handle any unexpected errors
                    return (imageUrl, null);
                }
            });

            var results = await Task.WhenAll(tasks);

            // Separate valid and invalid items
            foreach (var (imageUrl, item) in results)
            {
                if (item == null)
                {
                    invalidCategoryItems.Add(new { ImageUrl = imageUrl, Reason = MessageConstants.CANNOT_IDENTIFY_CATEGORY });
                }
                else
                {
                    newItems.Add(item);
                }
            }

            // Add all valid items to repository
            foreach (var item in newItems)
            {
                await _unitOfWork.ItemRepository.AddAsync(item);
            }

            await _unitOfWork.SaveAsync();

            // Collect IDs of successfully added items
            var successfulItemIds = newItems.Select(item => item.Id).ToList();

            if (invalidCategoryItems.Any())
            {
                // Return response with both successful and failed items
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status207MultiStatus,
                    Message = MessageConstants.ITEM_CREATE_PARTIAL_SUCCESS,
                    Data = new
                    {
                        SuccessfulItems = new
                        {
                            Count = successfulItemIds.Count,
                            ItemIds = successfulItemIds
                        },
                        FailedItems = new
                        {
                            Count = invalidCategoryItems.Count,
                            Items = invalidCategoryItems
                        }
                    }
                };
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.ITEM_CREATE_SUCCESS,
                Data = new
                {
                    Count = successfulItemIds.Count,
                    ItemIds = successfulItemIds
                }
            };
        }

        public async Task<BaseResponseModel> BulkCreateItemManual(BulkItemRequestManualModel bulkUploadModel)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(bulkUploadModel.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var invalidCategoryItems = new List<object>();
            var validItems = new List<Item>();

            // Validate all categories first and collect invalid ones
            foreach (var itemUpload in bulkUploadModel.ItemsUpload)
            {
                var category = await _unitOfWork.CategoryRepository.GetByIdAsync(itemUpload.CategoryId);
                if (category == null)
                {
                    invalidCategoryItems.Add(new
                    {
                        ImageUrl = itemUpload.ImageURLs,
                        CategoryId = itemUpload.CategoryId,
                        Reason = MessageConstants.CATEGORY_NOT_EXIST
                    });
                }
                else
                {
                    var newItem = new Item
                    {
                        Name = "Sop Item",
                        CategoryId = itemUpload.CategoryId,
                        UserId = bulkUploadModel.UserId,
                        ImgUrl = itemUpload.ImageURLs,
                        IsAnalyzed = false
                    };
                    validItems.Add(newItem);
                    await _unitOfWork.ItemRepository.AddAsync(newItem);
                }
            }

            await _unitOfWork.SaveAsync();

            // Collect IDs of successfully added items
            var successfulItemIds = validItems.Select(item => item.Id).ToList();

            if (invalidCategoryItems.Any())
            {
                // Return response with both successful and failed items
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status207MultiStatus,
                    Message = MessageConstants.ITEM_CREATE_PARTIAL_SUCCESS,
                    Data = new
                    {
                        SuccessfulItems = new
                        {
                            Count = successfulItemIds.Count,
                            ItemIds = successfulItemIds
                        },
                        FailedItems = new
                        {
                            Count = invalidCategoryItems.Count,
                            Items = invalidCategoryItems
                        }
                    }
                };
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.ITEM_CREATE_SUCCESS,
                Data = new
                {
                    Count = successfulItemIds.Count,
                    ItemIds = successfulItemIds
                }
            };
        }

        public async Task<BaseResponseModel> AnalysisItem(ItemModelRequest request)
        {
            var descriptionPromptSetting = await _unitOfWork.AISettingRepository.GetByTypeAsync(AISettingType.DESCRIPTION_ITEM_PROMPT);

            var styles = await _unitOfWork.StyleRepository.getAllStyleSystem();
            var occasions = await _unitOfWork.OccasionRepository.GetAllAsync();
            var seasons = await _unitOfWork.SeasonRepository.GetAllAsync();

            // JSON serializer options
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            //mapped to json
            var stylesJson = JsonSerializer.Serialize(_mapper.Map<List<StyleItemModel>>(styles), serializerOptions);
            var occasionsJson = JsonSerializer.Serialize(_mapper.Map<List<OccasionItemModel>>(occasions), serializerOptions);
            var seasonsJson = JsonSerializer.Serialize(_mapper.Map<List<SeasonItemModel>>(seasons), serializerOptions);

            var promptText = descriptionPromptSetting.Value;
            promptText = promptText.Replace("{{styles}}", stylesJson);
            promptText = promptText.Replace("{{occasions}}", occasionsJson);
            promptText = promptText.Replace("{{seasons}}", seasonsJson);

            // Step 1: Fetch all items first
            var items = new List<Item>();
            foreach (var itemId in request.ItemIds)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdAsync(itemId);
                if (item == null)
                {
                    throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
                }
                if ((bool)!item.IsAnalyzed)
                    items.Add(item);
            }

            // Step 2: Process all images in parallel (no DB operations here)
            var analysisResults = await Task.WhenAll(items.Select(async item =>
                  {
                      var client = _httpClientFactory.CreateClient("RembgClient");

                      var requestBody = new RembgRequest
                      {
                          Input = new RembgInput { Image = item.ImgUrl }
                      };

                      var responseRemBg = await client.PostAsJsonAsync("predictions", requestBody);


                      if (!responseRemBg.IsSuccessStatusCode)
                      {
                          throw new BadRequestException(MessageConstants.CALL_REM_BACKGROUND_FAIL);
                      }

                      var result = await responseRemBg.Content.ReadFromJsonAsync<RembgResponse>();

                      if (result == null || !string.Equals(result.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                      {
                          throw new BadRequestException(MessageConstants.REM_BACKGROUND_IMAGE_FAIL);
                      }

                      var fileRemoveBackground = ImageUtils.Base64ToFormFile(result.Output, item.ImgUrl.Split("/").Last());

                      var fileupload = await _minioService.UploadImageAsync(fileRemoveBackground);

                      if (fileupload?.Data is not ImageUploadResult uploadData || string.IsNullOrEmpty(uploadData.DownloadUrl))
                      {
                          throw new BadRequestException(MessageConstants.FILE_NOT_FOUND);
                      }

                      var imgResponse = await ImageUtils.ConvertToBase64Async(uploadData.DownloadUrl, _httpClientFactory.CreateClient("AnalysisClient"));
                      var analysis = await _geminiService.ImageGenerateContent(imgResponse.base64, imgResponse.mimetype, promptText);
                      analysis.ImgURL = uploadData.DownloadUrl;
                      _ = await _minioService.DeleteImageByURLAsync(item.ImgUrl);
                      return new
                      {
                          Item = item,
                          Analysis = analysis
                      };
                  }));

            // Step 3: Update all items with analysis results
            foreach (var result in analysisResults)
            {
                var item = result.Item;
                var analysis = result.Analysis;

                // Map basic fields
                item.Name = analysis.ItemName;
                item.ImgUrl = analysis.ImgURL;
                item.AiDescription = analysis.AiDescription;
                item.WeatherSuitable = analysis.WeatherSuitable;
                item.Condition = analysis.Condition;
                item.Pattern = analysis.Pattern;
                item.Fabric = analysis.Fabric;
                item.IsAnalyzed = true;
                item.AIConfidence = analysis.Confidence;
                item.Color = JsonSerializer.Serialize(analysis.Colors, serializerOptions);

                // Merge category from existing AIAnalyzeJson with new analysis
                long categoryId = item.CategoryId ?? 0;
                if (!string.IsNullOrEmpty(item.AIAnalyzeJson))
                {
                    try
                    {
                        var existingAnalysis = JsonSerializer.Deserialize<CategoryItemAnalysisModel>(item.AIAnalyzeJson, serializerOptions);
                        if (existingAnalysis != null && existingAnalysis.CategoryId != 0)
                        {
                            categoryId = existingAnalysis.CategoryId;
                        }
                    }
                    catch
                    {
                        // If deserialization fails, use current CategoryId
                    }
                }

                // Create complete AI analysis model
                var completeAnalysis = new ItemAIAnalysisModel
                {
                    CategoryId = categoryId,
                    Colors = analysis.Colors,
                    AiDescription = analysis.AiDescription,
                    WeatherSuitable = analysis.WeatherSuitable,
                    Condition = analysis.Condition,
                    Pattern = analysis.Pattern,
                    Fabric = analysis.Fabric,
                    Styles = analysis.Styles,
                    Occasions = analysis.Occasions,
                    Seasons = analysis.Seasons,
                    Confidence = analysis.Confidence
                };

                item.AIAnalyzeJson = JsonSerializer.Serialize(completeAnalysis, serializerOptions);

                _unitOfWork.ItemRepository.UpdateAsync(item);
            }

            // Save all item updates
            await _unitOfWork.SaveAsync();

            // Step 4: Update relationships for all items
            foreach (var result in analysisResults)
            {
                var item = result.Item;
                var analysis = result.Analysis;

                var styleIds = analysis.Styles?.Select(s => (long)s.Id).ToList() ?? new List<long>();
                var occasionIds = analysis.Occasions?.Select(o => (long)o.Id).ToList() ?? new List<long>();
                var seasonIds = analysis.Seasons?.Select(s => (long)s.Id).ToList() ?? new List<long>();

                await UpdateItemRelationshipsAsync(item.Id, styleIds, occasionIds, seasonIds);
            }

            // Step 5: Build confidence model response
            var itemConfidenceModel = analysisResults.Select(result => new ItemConfidenceModel
            {
                ItemId = (int)result.Item.Id,
                Confidence = result.Analysis.Confidence
            }).ToList();

            foreach (var item in analysisResults)
            {
                var newItemInclude = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(item.Item.Id,
                                            include: query => query
                                                    .Include(x => x.Category).ThenInclude(x => x.Parent)
                                                    .Include(x => x.User)
                                                    .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                                                    .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                                                    .Include(x => x.ItemStyles).ThenInclude(x => x.Style));

                var stringItem = ConvertItemToEmbeddingString(newItemInclude);

                var embeddingText = await _geminiService.EmbeddingText(stringItem);

                if (embeddingText != null && embeddingText.Any())
                {
                    // Prepare payload for Qdrant
                    var payload = new Dictionary<string, object>
                    {
                        { "UserId", newItemInclude.UserId ?? 0 },
                        { "Name", newItemInclude.Name ?? "" },
                        { "Category", newItemInclude.Category?.Parent.Name ?? "" },
                        { "Color", newItemInclude.Color ?? "" },
                        { "Brand", newItemInclude.Brand ?? "" }
                    };

                    // Upload to Qdrant
                    await _qdrantService.UpSertItem(embeddingText, payload, newItemInclude.Id);
                }
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_UPDATE_SUCCESS,
                Data = itemConfidenceModel
            };
        }

        public async Task<BaseResponseModel> GetUserStats(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var totalItems = await _unitOfWork.ItemRepository.CountItemByUserId(userId);

            var rootCategory = await _unitOfWork.CategoryRepository.GetAllParentCategory();

            Dictionary<string, int> categoryCounts = new Dictionary<string, int>();
            foreach (var item in rootCategory)
            {
                var result = await _unitOfWork.ItemRepository.CountItemByUserIdAndCategoryParent(userId, item.Id);
                var dickey = categoryCounts.ContainsKey(item.Name);
                if (!dickey)
                {
                    categoryCounts.Add(item.Name, result);
                }
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_USER_STATS_SUCCESS,
                Data = new
                {
                    TotalItems = totalItems,
                    CategoryCounts = categoryCounts
                } 
            };

        }
    }
}
