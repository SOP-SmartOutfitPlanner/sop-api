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
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IHttpClientFactory _httpClientFactory;

        public ItemService(IUnitOfWork unitOfWork, IMapper mapper, IGeminiService geminiService, IFirebaseStorageService firebaseStorageService, IHttpClientFactory httpClientFactory)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _geminiService = geminiService;
            _firebaseStorageService = firebaseStorageService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<BaseResponseModel> AddNewItem(ItemCreateModel model)
        {
            if (await _unitOfWork.ItemRepository.ExistsByNameAsync(model.Name, model.UserId))
                throw new BadRequestException(MessageConstants.ITEM_ALREADY_EXISTS);

            var entity = _mapper.Map<Item>(model);
            await _unitOfWork.ItemRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.ITEM_CREATE_SUCCESS,
                Data = _mapper.Map<ItemModel>(entity)
            };
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

        public async Task<BaseResponseModel> GetItemByIdAsync(long id)
        {
            var entity = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(
                id,
                filter: x => !x.IsDeleted,
                include: q => q.Include(x => x.Category)
                               .Include(x => x.User)
            );

            if (entity == null) throw new NotFoundException(MessageConstants.ITEM_NOT_EXISTED);

            var data = _mapper.Map<ItemModel>(entity);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_GET_SUCCESS,
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

            var newItemInclude = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(newItem.Id, include: query => query.Include(x => x.Category).Include(x => x.UserId));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_CREATE_SUCCESS,
                Data = _mapper.Map<ItemModel>(newItemInclude)
            };
        public async Task<BaseResponseModel> GetItemsAsync(long userId, PaginationParameter pagination)
        {
            var page = await _unitOfWork.ItemRepository.ToPaginationIncludeAsync(
                pagination,
                filter: x => !x.IsDeleted && x.UserId == userId,
                include: q => q.Include(x => x.Category)
            );

            var items = page.Select(x => _mapper.Map<ItemModel>(x)).ToList();

            var meta = new
            {
                PageIndex = page.CurrentPage,
                PageSize = page.PageSize,
                page.TotalCount,
                page.TotalPages
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_GET_SUCCESS,
                Data = new ModelPaging
                {
                    Data = items,
                    MetaData = meta
                }
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

        public async Task<BaseResponseModel> GetSummaryItem(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException(MessageConstants.IMAGE_IS_NOT_VALID);
            }

            //convert image to base64
            var base64Image = await ImageUtils.ConvertToBase64Async(file);

            //validation image
            bool isValid = await _geminiService.ImageValidation(base64Image, file.ContentType);

            if (!isValid)
            {
                throw new BadRequestException(MessageConstants.IMAGE_IS_NOT_VALID);
            }

            //upload image to firebase storage
            var uploadToRemoveBg = await UploadFileToFirebase(file);

            var imageDownloadUrl = uploadToRemoveBg.DownloadUrl;

            //call rembg service to remove background and convert to formfile
            var fileRemoveBackground = await CallRembgAndGetRemovedFile(imageDownloadUrl, file.FileName, uploadToRemoveBg.FullPath);

            //call gemini service to get summary
            var summaryFromGemini = await _geminiService.ImageGenerateContent(await ImageUtils.ConvertToBase64Async(fileRemoveBackground), fileRemoveBackground.ContentType);

            //upload image remove background to firebase storage
            var imageRemBgResponse = await UploadFileToFirebase(fileRemoveBackground);

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
        private async Task<ImageUploadResult> UploadFileToFirebase(IFormFile file)
        {
            var uploadResult = await _firebaseStorageService.UploadImageAsync(file);
            if (uploadResult?.Data is not ImageUploadResult uploadData || string.IsNullOrEmpty(uploadData.DownloadUrl))
            {
                throw new BadRequestException(MessageConstants.FILE_NOT_FOUND);
            }

            return uploadData;
        }

        // Helper: call rembg service and return IFormFile of removed background image
        private async Task<IFormFile> CallRembgAndGetRemovedFile(string imageUrl, string originalFileName, string fullPathToDelete)
        {
            var client = _httpClientFactory.CreateClient("RembgClient");

            var requestBody = new RembgRequest
            {
                Input = new RembgInput { Image = imageUrl }
            };

            var responseRemBg = await client.PostAsJsonAsync("predictions", requestBody);

            // delete original uploaded image regardless of rembg result
            await _firebaseStorageService.DeleteImageAsync(fullPathToDelete);

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
    }
}
