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
                include: query => query.Include(x => x.Category).Include(x => x.User),
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
    }
}
