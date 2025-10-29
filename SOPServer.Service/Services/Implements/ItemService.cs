using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.FirebaseModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.RemBgModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class ItemService : IItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IGeminiService _geminiService;
        private readonly IMinioService _minioService;
        private readonly IHttpClientFactory _httpClientFactory;

        public ItemService(IUnitOfWork unitOfWork, IMapper mapper, IGeminiService geminiService, IMinioService minioService, IHttpClientFactory httpClientFactory)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _geminiService = geminiService;
            _minioService = minioService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<BaseResponseModel> UpdateItemAsync(long id, ItemCreateModel model)
        {
            var entity = await _unitOfWork.ItemRepository.GetByIdAsync(id);
            if (entity == null) throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);

            if (await _unitOfWork.ItemRepository.ExistsByNameAsync(model.Name, model.UserId, id))
                throw new BadRequestException(MessageConstants.ITEM_ALREADY_EXISTS);

            _mapper.Map(model, entity);
            _unitOfWork.ItemRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            var data = _mapper.Map<ItemModel>(entity);
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

            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(model.CategoryId);
            if (category == null)
            {
                throw new NotFoundException(MessageConstants.CATEGORY_NOT_EXIST);
            }

            var newItem = _mapper.Map<Item>(model);

            await _unitOfWork.ItemRepository.AddAsync(newItem);
            _unitOfWork.Save();

            var newItemInclude = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(newItem.Id, include: query => query.Include(x => x.Category).Include(x => x.User));

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

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DELETE_ITEM_SUCCESS
            };
        }

        public async Task<BaseResponseModel> GetItemById(long id)
        {
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(id, include: query => query.Include(x => x.Category).Include(x => x.User));
            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_NOT_EXISTED,
                Data = _mapper.Map<ItemModel>(item)
            };
        }

        public async Task<BaseResponseModel> GetItemByUserPaginationAsync(PaginationParameter paginationParameter, long userId)
        {
            var items = await _unitOfWork.ItemRepository.ToPaginationIncludeAsync(paginationParameter,
                include: query => query.Include(x => x.Category).Include(x => x.User),
                filter: x => x.UserId == userId,
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

        public async Task<BaseResponseModel> GetItemPaginationAsync(PaginationParameter paginationParameter)
        {
            var items = await _unitOfWork.ItemRepository.ToPaginationIncludeAsync(paginationParameter,
                include: query => query.Include(x => x.Category).Include(x => x.User).Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion),
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

        public async Task<BaseResponseModel> GetAnalyzeItem(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new BadRequestException(MessageConstants.IMAGE_IS_NOT_VALID);

            // 1️⃣ Khởi tạo task song song
            var convertBase64Task = ImageUtils.ConvertToBase64Async(file);
            var uploadOriginalTask = UploadFileToMinio(file);

            // 2️⃣ Chờ cả 2 cùng xong
            await Task.WhenAll(convertBase64Task, uploadOriginalTask);

            var base64Image = await convertBase64Task;
            var uploadToMinio = await uploadOriginalTask;

            // 3️⃣ Validation image
            var validation = await _geminiService.ImageValidation(base64Image, file.ContentType);
            if (!validation.IsValid)
                throw new BadRequestException(validation.Message);

            // 4️⃣ Remove background (phụ thuộc uploadToMinio)
            var fileRemoveBackground = await CallRembgAndGetRemovedFile(
                uploadToMinio.DownloadUrl,
                uploadToMinio.FileName
            );

            // 5️⃣ Song song 2 tác vụ sau khi đã có file remove background:
            //     - Convert base64 cho file remove background
            //     - Upload file remove background lên MinIO
            var convertRemovedBase64Task = ImageUtils.ConvertToBase64Async(fileRemoveBackground);
            var uploadRemovedFileTask = UploadFileToMinio(fileRemoveBackground);

            await Task.WhenAll(convertRemovedBase64Task, uploadRemovedFileTask);

            var base64Removed = await convertRemovedBase64Task;
            var imageRemBgResponse = await uploadRemovedFileTask;

            // 6️⃣ Gọi Gemini summary (chờ base64Removed)
            var summaryFromGemini = await _geminiService.ImageGenerateContent(
                base64Removed, fileRemoveBackground.ContentType
            );

            // 7️⃣ Map và trả response
            var itemSummary = _mapper.Map<ItemSummaryModel>(summaryFromGemini);
            itemSummary.ImageRemBgURL = imageRemBgResponse.DownloadUrl;

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_SUMMARY_IMAGE_SUCCESS,
                Data = itemSummary
            };
        }

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

        public async Task<BaseResponseModel> AddOccasionsToItemAsync(AddOccasionsToItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
                include: query => query.Include(x => x.ItemOccasions));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate all occasions exist and store them
            var validOccasions = new List<Occasion>();
            foreach (var occasionId in model.OccasionIds)
            {
                var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(occasionId);
                if (occasion == null)
                {
                    throw new NotFoundException($"{MessageConstants.OCCASION_NOT_EXIST}: {occasionId}");
                }
                validOccasions.Add(occasion);
            }

            // Get existing occasion IDs for this item (non-deleted)
            var existingOccasionIds = item.ItemOccasions
                .Where(io => !io.IsDeleted)
                .Select(io => io.OccasionId ?? 0)
                .ToList();

            // Find occasions to add (not already associated)
            var occasionsToAdd = validOccasions
                .Where(o => !existingOccasionIds.Contains(o.Id))
                .ToList();

            if (!occasionsToAdd.Any())
            {
                throw new BadRequestException(MessageConstants.ITEM_OCCASION_ALREADY_EXISTS);
            }

            // Add new item-occasion relationships
            foreach (var occasion in occasionsToAdd)
            {
                var itemOccasion = new ItemOccasion
                {
                    ItemId = model.ItemId,
                    OccasionId = occasion.Id
                };
                await _unitOfWork.ItemOccasionRepository.AddAsync(itemOccasion);
            }

            await _unitOfWork.SaveAsync();

            // Build response with added occasions
            var response = new AddOccasionsToItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                AddedOccasions = occasionsToAdd.Select(o => new AddedOccasionModel
                {
                    Id = o.Id,
                    Name = o.Name
                }).ToList()
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ADD_OCCASIONS_TO_ITEM_SUCCESS,
                Data = response
            };
        }

        public async Task<BaseResponseModel> RemoveOccasionFromItemAsync(RemoveOccasionFromItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
      include: query => query.Include(x => x.ItemOccasions.Where(io => !io.IsDeleted))
   .ThenInclude(io => io.Occasion));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate occasion exists
            var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(model.OccasionId);
            if (occasion == null)
            {
                throw new NotFoundException(MessageConstants.OCCASION_NOT_EXIST);
            }

            // Find the item-occasion relationship
            var itemOccasion = item.ItemOccasions
                      .FirstOrDefault(io => io.OccasionId == model.OccasionId && !io.IsDeleted);

            if (itemOccasion == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_OCCASION_NOT_FOUND);
            }

            // Soft delete the item-occasion relationship
            _unitOfWork.ItemOccasionRepository.SoftDeleteAsync(itemOccasion);
            await _unitOfWork.SaveAsync();

            // Build response
            var response = new RemoveOccasionFromItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                RemovedOccasionId = occasion.Id,
                RemovedOccasionName = occasion.Name
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.REMOVE_OCCASION_FROM_ITEM_SUCCESS,
                Data = response
            };
        }

        public async Task<BaseResponseModel> ReplaceOccasionsForItemAsync(ReplaceOccasionsForItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
              include: query => query.Include(x => x.ItemOccasions));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate all new occasions exist and store them
            var newOccasions = new List<Occasion>();
            foreach (var occasionId in model.OccasionIds)
            {
                var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(occasionId);
                if (occasion == null)
                {
                    throw new NotFoundException($"{MessageConstants.OCCASION_NOT_EXIST}: {occasionId}");
                }
                newOccasions.Add(occasion);
            }

            // Soft delete all existing item-occasion relationships
            var existingItemOccasions = item.ItemOccasions.Where(io => !io.IsDeleted).ToList();
            foreach (var existingItemOccasion in existingItemOccasions)
            {
                _unitOfWork.ItemOccasionRepository.SoftDeleteAsync(existingItemOccasion);
            }

            // Add new item-occasion relationships
            foreach (var occasion in newOccasions)
            {
                var itemOccasion = new ItemOccasion
                {
                    ItemId = model.ItemId,
                    OccasionId = occasion.Id
                };
                await _unitOfWork.ItemOccasionRepository.AddAsync(itemOccasion);
            }

            await _unitOfWork.SaveAsync();

            // Build response with replaced occasions
            var response = new ReplaceOccasionsForItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                ReplacedOccasions = newOccasions.Select(o => new AddedOccasionModel
                {
                    Id = o.Id,
                    Name = o.Name
                }).ToList()
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.REPLACE_OCCASIONS_FOR_ITEM_SUCCESS,
                Data = response
            };
        }

        public async Task<BaseResponseModel> AddStylesToItemAsync(AddStylesToItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
                include: query => query.Include(x => x.ItemStyles));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate all styles exist and store them
            var validStyles = new List<Style>();
            foreach (var styleId in model.StyleIds)
            {
                var style = await _unitOfWork.StyleRepository.GetByIdAsync(styleId);
                if (style == null)
                {
                    throw new NotFoundException($"{MessageConstants.STYLE_NOT_EXIST}: {styleId}");
                }
                validStyles.Add(style);
            }

            // Get existing style IDs for this item (non-deleted)
            var existingStyleIds = item.ItemStyles
            .Where(ist => !ist.IsDeleted)
                .Select(ist => ist.StyleId ?? 0)
    .ToList();

            // Find styles to add (not already associated)
            var stylesToAdd = validStyles
                    .Where(s => !existingStyleIds.Contains(s.Id))
               .ToList();

            if (!stylesToAdd.Any())
            {
                throw new BadRequestException(MessageConstants.ITEM_STYLE_ALREADY_EXISTS);
            }

            // Add new item-style relationships
            foreach (var style in stylesToAdd)
            {
                var itemStyle = new ItemStyle
                {
                    ItemId = model.ItemId,
                    StyleId = style.Id
                };
                await _unitOfWork.ItemStyleRepository.AddAsync(itemStyle);
            }

            await _unitOfWork.SaveAsync();

            // Build response with added styles
            var response = new AddStylesToItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                AddedStyles = stylesToAdd.Select(s => new AddedStyleModel
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList()
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ADD_STYLES_TO_ITEM_SUCCESS,
                Data = response
            };
        }

        public async Task<BaseResponseModel> RemoveStyleFromItemAsync(RemoveStyleFromItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
      include: query => query.Include(x => x.ItemStyles.Where(ist => !ist.IsDeleted))
    .ThenInclude(ist => ist.Style));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate style exists
            var style = await _unitOfWork.StyleRepository.GetByIdAsync(model.StyleId);
            if (style == null)
            {
                throw new NotFoundException(MessageConstants.STYLE_NOT_EXIST);
            }

            // Find the item-style relationship
            var itemStyle = item.ItemStyles
             .FirstOrDefault(ist => ist.StyleId == model.StyleId && !ist.IsDeleted);

            if (itemStyle == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_STYLE_NOT_FOUND);
            }

            // Soft delete the item-style relationship
            _unitOfWork.ItemStyleRepository.SoftDeleteAsync(itemStyle);
            await _unitOfWork.SaveAsync();

            // Build response
            var response = new RemoveStyleFromItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                RemovedStyleId = style.Id,
                RemovedStyleName = style.Name
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.REMOVE_STYLE_FROM_ITEM_SUCCESS,
                Data = response
            };
        }

        public async Task<BaseResponseModel> ReplaceStylesForItemAsync(ReplaceStylesForItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
 include: query => query.Include(x => x.ItemStyles));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate all new styles exist and store them
            var newStyles = new List<Style>();
            foreach (var styleId in model.StyleIds)
            {
                var style = await _unitOfWork.StyleRepository.GetByIdAsync(styleId);
                if (style == null)
                {
                    throw new NotFoundException($"{MessageConstants.STYLE_NOT_EXIST}: {styleId}");
                }
                newStyles.Add(style);
            }

            // Soft delete all existing item-style relationships
            var existingItemStyles = item.ItemStyles.Where(ist => !ist.IsDeleted).ToList();
            foreach (var existingItemStyle in existingItemStyles)
            {
                _unitOfWork.ItemStyleRepository.SoftDeleteAsync(existingItemStyle);
            }

            // Add new item-style relationships
            foreach (var style in newStyles)
            {
                var itemStyle = new ItemStyle
                {
                    ItemId = model.ItemId,
                    StyleId = style.Id
                };
                await _unitOfWork.ItemStyleRepository.AddAsync(itemStyle);
            }

            await _unitOfWork.SaveAsync();

            // Build response with replaced styles
            var response = new ReplaceStylesForItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                ReplacedStyles = newStyles.Select(s => new AddedStyleModel
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList()
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.REPLACE_STYLES_FOR_ITEM_SUCCESS,
                Data = response
            };
        }

        public async Task<BaseResponseModel> AddSeasonsToItemAsync(AddSeasonsToItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
   include: query => query.Include(x => x.ItemSeasons));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate all seasons exist and store them
            var validSeasons = new List<Season>();
            foreach (var seasonId in model.SeasonIds)
            {
                var season = await _unitOfWork.SeasonRepository.GetByIdAsync(seasonId);
                if (season == null)
                {
                    throw new NotFoundException($"{MessageConstants.SEASON_NOT_EXIST}: {seasonId}");
                }
                validSeasons.Add(season);
            }

            // Get existing season IDs for this item (non-deleted)
            var existingSeasonIds = item.ItemSeasons
               .Where(itemSeason => !itemSeason.IsDeleted)
                   .Select(itemSeason => itemSeason.SeasonId ?? 0)
                .ToList();

            // Find seasons to add (not already associated)
            var seasonsToAdd = validSeasons
               .Where(s => !existingSeasonIds.Contains(s.Id))
                       .ToList();

            if (!seasonsToAdd.Any())
            {
                throw new BadRequestException(MessageConstants.ITEM_SEASON_ALREADY_EXISTS);
            }

            // Add new item-season relationships
            foreach (var season in seasonsToAdd)
            {
                var itemSeason = new ItemSeason
                {
                    ItemId = model.ItemId,
                    SeasonId = season.Id
                };
                await _unitOfWork.ItemSeasonRepository.AddAsync(itemSeason);
            }

            await _unitOfWork.SaveAsync();

            // Build response with added seasons
            var response = new AddSeasonsToItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                AddedSeasons = seasonsToAdd.Select(s => new AddedSeasonModel
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList()
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ADD_SEASONS_TO_ITEM_SUCCESS,
                Data = response
            };
        }

        public async Task<BaseResponseModel> RemoveSeasonFromItemAsync(RemoveSeasonFromItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
             include: query => query.Include(x => x.ItemSeasons.Where(itemSeason => !itemSeason.IsDeleted))
               .ThenInclude(itemSeason => itemSeason.Season));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate season exists
            var season = await _unitOfWork.SeasonRepository.GetByIdAsync(model.SeasonId);
            if (season == null)
            {
                throw new NotFoundException(MessageConstants.SEASON_NOT_EXIST);
            }

            // Find the item-season relationship
            var itemSeason = item.ItemSeasons
      .FirstOrDefault(itemSeason => itemSeason.SeasonId == model.SeasonId && !itemSeason.IsDeleted);

            if (itemSeason == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_SEASON_NOT_FOUND);
            }

            // Soft delete the item-season relationship
            _unitOfWork.ItemSeasonRepository.SoftDeleteAsync(itemSeason);
            await _unitOfWork.SaveAsync();

            // Build response
            var response = new RemoveSeasonFromItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                RemovedSeasonId = season.Id,
                RemovedSeasonName = season.Name
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.REMOVE_SEASON_FROM_ITEM_SUCCESS,
                Data = response
            };
        }

        public async Task<BaseResponseModel> ReplaceSeasonsForItemAsync(ReplaceSeasonsForItemModel model)
        {
            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(model.ItemId,
                    include: query => query.Include(x => x.ItemSeasons));

            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);
            }

            // Validate all new seasons exist and store them
            var newSeasons = new List<Season>();
            foreach (var seasonId in model.SeasonIds)
            {
                var season = await _unitOfWork.SeasonRepository.GetByIdAsync(seasonId);
                if (season == null)
                {
                    throw new NotFoundException($"{MessageConstants.SEASON_NOT_EXIST}: {seasonId}");
                }
                newSeasons.Add(season);
            }

            // Soft delete all existing item-season relationships
            var existingItemSeasons = item.ItemSeasons.Where(itemSeason => !itemSeason.IsDeleted).ToList();
            foreach (var existingItemSeason in existingItemSeasons)
            {
                _unitOfWork.ItemSeasonRepository.SoftDeleteAsync(existingItemSeason);
            }

            // Add new item-season relationships
            foreach (var season in newSeasons)
            {
                var itemSeason = new ItemSeason
                {
                    ItemId = model.ItemId,
                    SeasonId = season.Id
                };
                await _unitOfWork.ItemSeasonRepository.AddAsync(itemSeason);
            }

            await _unitOfWork.SaveAsync();

            // Build response with replaced seasons
            var response = new ReplaceSeasonsForItemResponseModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                ReplacedSeasons = newSeasons.Select(s => new AddedSeasonModel
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList()
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.REPLACE_SEASONS_FOR_ITEM_SUCCESS,
                Data = response
            };
        }
    }
}
