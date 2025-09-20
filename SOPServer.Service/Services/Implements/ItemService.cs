using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class ItemService : IItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IGeminiService _geminiService;

        public ItemService(IUnitOfWork unitOfWork, IMapper mapper, IGeminiService geminiService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _geminiService = geminiService;
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
        }

        public async Task<BaseResponseModel> DeleteItemByIdAsync(long id)
        {
            var item = await _unitOfWork.ItemRepository.GetByIdAsync(id);

            if(item == null)
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
                orderBy: x=> x.OrderByDescending(x => x.CreatedDate));

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

            var base64Image = await AIUtils.ConvertToBase64Async(file);

            bool isValid = await _geminiService.ImageValidation(base64Image, file.ContentType);

            if (!isValid)
            {
                throw new BadRequestException(MessageConstants.IMAGE_IS_NOT_VALID);
            }

            var response = await _geminiService.ImageGenerateContent(base64Image, file.ContentType);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.IMAGE_IS_VALID,
                Data = response
            };
        }
    }
}
