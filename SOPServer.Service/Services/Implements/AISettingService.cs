using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.AISettingModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class AISettingService : IAISettingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AISettingService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetAllAsync()
        {
            var list = await _unitOfWork.AISettingRepository.GetAllAsync();
            var result = _mapper.Map<IEnumerable<AISettingModel>>(list);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_AISETTING_SUCCESS,
                Data = result
            };
        }

        public async Task<BaseResponseModel> GetByIdAsync(long id)
        {
            var entity = await _unitOfWork.AISettingRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException(MessageConstants.AISETTING_NOT_EXIST);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_AISETTING_SUCCESS,
                Data = _mapper.Map<AISettingModel>(entity)
            };
        }

        public async Task<BaseResponseModel> GetByTypeAsync(AISettingType type)
        {
            var entity = await _unitOfWork.AISettingRepository.GetByTypeAsync(type);
            if (entity == null)
            {
                throw new NotFoundException(MessageConstants.AISETTING_NOT_EXIST);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_AISETTING_SUCCESS,
                Data = entity
            };
        }

        public async Task<BaseResponseModel> CreateAsync(AISettingRequestModel model)
        {
            var existed = await _unitOfWork.AISettingRepository.GetByTypeAsync(model.Type);
            if (existed != null)
            {
                throw new BadRequestException(MessageConstants.AISETTING_ALREADY_EXIST);
            }

            var entity = _mapper.Map<AISetting>(model);
            await _unitOfWork.AISettingRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            var result = _mapper.Map<AISettingModel>(entity);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.AISETTING_CREATE_SUCCESSFULLY,
                Data = result
            };
        }

        public async Task<BaseResponseModel> UpdateAsync(long id, AISettingRequestModel model)
        {
            var existing = await _unitOfWork.AISettingRepository.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException(MessageConstants.AISETTING_NOT_EXIST);
            }

            existing.Name = model.Name;
            existing.Value = model.Value;
            existing.Type = model.Type;
            existing.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.AISettingRepository.UpdateAsync(existing);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.AISETTING_UPDATE_SUCCESSFULLY,
                Data = existing
            };
        }

        public async Task<BaseResponseModel> DeleteAsync(long id)
        {
            var entity = await _unitOfWork.AISettingRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException(MessageConstants.AISETTING_NOT_EXIST);
            }

            _unitOfWork.AISettingRepository.SoftDeleteAsync(entity);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.AISETTING_DELETE_SUCCESSFULLY,
            };
        }
    }
}
