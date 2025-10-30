using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.JobModels;
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
    public class JobService : IJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public JobService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetAllAsync(string? search = null)
        {
            IEnumerable<Job> list;

            if (string.IsNullOrWhiteSpace(search))
            {
                list = await _unitOfWork.JobRepository.GetAllAsync();
            }
            else
            {
                list = await _unitOfWork.JobRepository.SearchByNameAsync(search);
            }

            var result = _mapper.Map<IEnumerable<JobModel>>(list);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_JOB_SUCCESS,
                Data = result
            };
        }

        public async Task<BaseResponseModel> GetByIdAsync(long id)
        {
            var entity = await _unitOfWork.JobRepository.GetByIdAsync(id);
            if (entity == null)
                throw new NotFoundException(MessageConstants.JOB_NOT_EXIST);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_JOB_SUCCESS,
                Data = _mapper.Map<JobModel>(entity)
            };
        }

        public async Task<BaseResponseModel> CreateAsync(JobRequestModel model)
        {
            var existed = await _unitOfWork.JobRepository.GetByNameAsync(model.Name);
            if (existed != null)
            {
                throw new BadRequestException(MessageConstants.JOB_ALREADY_EXIST);
            }

            var entity = _mapper.Map<Job>(model);
            await _unitOfWork.JobRepository.AddAsync(entity);
            await _unitOfWork.SaveAsync();

            var result = _mapper.Map<JobModel>(entity);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.JOB_CREATE_SUCCESS,
                Data = result
            };
        }

        public async Task<BaseResponseModel> UpdateAsync(long id, JobRequestModel model)
        {
            var existing = await _unitOfWork.JobRepository.GetByIdAsync(id);
            if (existing == null)
            {
                throw new NotFoundException(MessageConstants.JOB_NOT_EXIST);
            }

            // Check if another job with the same name exists
            var existingName = await _unitOfWork.JobRepository.GetByNameAsync(model.Name);
            if (existingName != null && existingName.Id != id)
            {
                throw new BadRequestException(MessageConstants.JOB_ALREADY_EXIST);
            }

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.JobRepository.UpdateAsync(existing);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.JOB_UPDATE_SUCCESS,
                Data = _mapper.Map<JobModel>(existing)
            };
        }

        public async Task<BaseResponseModel> DeleteAsync(long id)
        {
            var entity = await _unitOfWork.JobRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new NotFoundException(MessageConstants.JOB_NOT_EXIST);
            }

            _unitOfWork.JobRepository.SoftDeleteAsync(entity);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.JOB_DELETE_SUCCESS,
            };
        }
    }
}
