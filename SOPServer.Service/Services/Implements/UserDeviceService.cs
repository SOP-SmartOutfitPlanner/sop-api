using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserDeviceModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class UserDeviceService : IUserDeviceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserDeviceService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> AddDeviceTokenByUserId(CreateUserDeviceModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var existingDevice = await _unitOfWork.UserDeviceRepository.GetByTokenDevice(model.DeviceToken);
            if (existingDevice != null)
            {
                // Update the existing device token if it belongs to a different user
                if (existingDevice.UserId != model.UserId)
                {
                    existingDevice.UserId = model.UserId;
                    _unitOfWork.UserDeviceRepository.UpdateAsync(existingDevice);
                    await _unitOfWork.SaveAsync();
                }

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.DEVICE_TOKEN_UPDATED_SUCCESS,
                    Data = _mapper.Map<UserDeviceModel>(existingDevice)
                };
            }

            var newUserDevice = _mapper.Map<UserDevice>(model);
            await _unitOfWork.UserDeviceRepository.AddAsync(newUserDevice);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.DEVICE_TOKEN_ADD_SUCCESS,
                Data = _mapper.Map<UserDeviceModel>(newUserDevice)
            };
        }

        public async Task<BaseResponseModel> DeleteDeviceToken(string token)
        {
            var userDevice = await _unitOfWork.UserDeviceRepository.GetByTokenDevice(token);
            if (userDevice == null)
            {
                throw new NotFoundException(MessageConstants.DEVICE_TOKEN_NOT_EXIST);
            }

            _unitOfWork.UserDeviceRepository.SoftDeleteAsync(userDevice);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DEVICE_TOKEN_DELETE_SUCCESS,
            };
        }
    }
}
