using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

        public async Task<BaseResponseModel> GetSummaryItem(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException(MessageConstants.IMAGE_IS_NOT_VALID);
            }

            //convert image to base64
            var base64Image = await ImageUtils.ConvertToBase64Async(file);

            //validation image
            var validation = await _geminiService.ImageValidation(base64Image, file.ContentType);
            if (!validation.IsValid)
            {
                throw new BadRequestException(validation.Message);
            }

            //upload image to firebase storage
            var uploadToRemoveBg = await UploadFileToMinio(file);

            //call rembg service to remove background and convert to formfile
            var fileRemoveBackground = await CallRembgAndGetRemovedFile(uploadToRemoveBg.DownloadUrl, uploadToRemoveBg.FileName);

            //call gemini service to get summary
            var summaryFromGemini = await _geminiService.ImageGenerateContent(await ImageUtils.ConvertToBase64Async(fileRemoveBackground), fileRemoveBackground.ContentType);

            //upload image remove background to firebase storage
            var imageRemBgResponse = await UploadFileToMinio(fileRemoveBackground);

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
    }
}
