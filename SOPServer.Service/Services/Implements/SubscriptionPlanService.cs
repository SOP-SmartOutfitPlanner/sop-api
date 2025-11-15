using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SubscriptionPlanModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SubscriptionPlanService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetAllAsync()
        {
            var list = await _unitOfWork.SubscriptionPlanRepository.GetAllAsync();
            var result = _mapper.Map<IEnumerable<SubscriptionPlanModel>>(list);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_SUBSCRIPTION_PLAN_SUCCESS,
                Data = result
            };
        }

        public async Task<BaseResponseModel> GetByIdAsync(long id)
        {
            var entity = await _unitOfWork.SubscriptionPlanRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException(MessageConstants.SUBSCRIPTION_PLAN_NOT_FOUND);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.SUBSCRIPTION_PLAN_GET_SUCCESS,
                Data = _mapper.Map<SubscriptionPlanModel>(entity)
            };
        }

        public async Task<BaseResponseModel> CreateAsync(SubscriptionPlanRequestModel model)
        {
            // Check if plan with same name exists
            var existingPlan = (await _unitOfWork.SubscriptionPlanRepository.GetAllAsync())
                .FirstOrDefault(p => p.Name.ToLower() == model.Name.ToLower());

            if (existingPlan != null)
                throw new BadRequestException(MessageConstants.SUBSCRIPTION_PLAN_NAME_EXISTS);

            var entity = _mapper.Map<SubscriptionPlan>(model);
            await _unitOfWork.SubscriptionPlanRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.SUBSCRIPTION_PLAN_CREATE_SUCCESS,
                Data = _mapper.Map<SubscriptionPlanModel>(entity)
            };
        }

        public async Task<BaseResponseModel> UpdateAsync(long id, SubscriptionPlanRequestModel model)
        {
            var entity = await _unitOfWork.SubscriptionPlanRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException(MessageConstants.SUBSCRIPTION_PLAN_NOT_FOUND);

            // Check if another plan with same name exists
            var existingPlan = (await _unitOfWork.SubscriptionPlanRepository.GetAllAsync())
                .FirstOrDefault(p => p.Name.ToLower() == model.Name.ToLower() && p.Id != id);

            if (existingPlan != null)
                throw new BadRequestException(MessageConstants.SUBSCRIPTION_PLAN_NAME_EXISTS);

            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.Price = model.Price;
            entity.BenefitLimit = model.BenefitLimit;

            _unitOfWork.SubscriptionPlanRepository.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.SUBSCRIPTION_PLAN_UPDATE_SUCCESS,
                Data = _mapper.Map<SubscriptionPlanModel>(entity)
            };
        }

        public async Task<BaseResponseModel> DeleteAsync(long id)
        {
            var entity = await _unitOfWork.SubscriptionPlanRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException(MessageConstants.SUBSCRIPTION_PLAN_NOT_FOUND);

            _unitOfWork.SubscriptionPlanRepository.SoftDeleteAsync(entity);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.SUBSCRIPTION_PLAN_DELETE_SUCCESS
            };
        }
    }
}
