using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.FollowerModels;
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
    public class FollowerService : IFollowerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FollowerService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> FollowUser(CreateFollowerModel model)
        {
            // Check if trying to follow self
            if (model.FollowerId == model.FollowingId)
            {
                throw new BadRequestException(MessageConstants.CANNOT_FOLLOW_YOURSELF);
            }
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.FollowerId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }
            // Check if user exists
            var followingUser = await _unitOfWork.UserRepository.GetByIdAsync(model.FollowingId);
            if (followingUser == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Check if already following
            var followExists = await _unitOfWork.FollowerRepository.GetByFollowerAndFollowing(model.FollowerId, model.FollowingId);
            if (followExists != null)
            {
                throw new BadRequestException(MessageConstants.ALREADY_FOLLOWING);
            }

            var newFollow = _mapper.Map<Follower>(model);
            await _unitOfWork.FollowerRepository.AddAsync(newFollow);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.FOLLOW_USER_SUCCESS,
                Data = _mapper.Map<FollowerModel>(newFollow)
            };
        }

        public async Task<BaseResponseModel> UnfollowUser(long id)
        {
            var followExists = await _unitOfWork.FollowerRepository.GetByIdAsync(id);
            if (followExists == null)
            {
                throw new NotFoundException(MessageConstants.FOLLOWER_NOT_FOUND);
            }

            _unitOfWork.FollowerRepository.SoftDeleteAsync(followExists);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.UNFOLLOW_USER_SUCCESS
            };
        }

        public async Task<BaseResponseModel> GetFollowerCount(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var count = await _unitOfWork.FollowerRepository.GetFollowerCount(userId);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_FOLLOWER_COUNT_SUCCESS,
                Data = new { Count = count }
            };
        }

        public async Task<BaseResponseModel> GetFollowingCount(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var count = await _unitOfWork.FollowerRepository.GetFollowingCount(userId);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_FOLLOWING_COUNT_SUCCESS,
                Data = new { Count = count }
            };
        }

        public async Task<BaseResponseModel> IsFollowing(long followerId, long followingId)
        {
            var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(followerId, followingId);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.CHECK_FOLLOWING_STATUS_SUCCESS,
                Data = new { IsFollowing = isFollowing }
            };
        }
    }
}
