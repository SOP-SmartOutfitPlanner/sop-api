using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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

        public ItemService(IUnitOfWork unitOfWork, IMapper mapper, IGeminiService geminiService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _geminiService = geminiService;
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
