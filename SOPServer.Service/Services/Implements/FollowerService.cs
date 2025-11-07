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

        public async Task<BaseResponseModel> ToggleFollowUser(CreateFollowerModel model)
        {
            // Check if trying to follow self
            if (model.FollowerId == model.FollowingId)
            {
                throw new BadRequestException(MessageConstants.CANNOT_FOLLOW_YOURSELF);
            }

            // Check if both users exist
            var followerUser = await _unitOfWork.UserRepository.GetByIdAsync(model.FollowerId);
            if (followerUser == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }
            var followingUser = await _unitOfWork.UserRepository.GetByIdAsync(model.FollowingId);
            if (followingUser == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var followRelationship = await _unitOfWork.FollowerRepository.GetByFollowerAndFollowingIncludeDeleted(model.FollowerId, model.FollowingId);

            if (followRelationship == null)
            {
                // Create new follow relationship
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

            // Toggle IsDeleted status
            followRelationship.IsDeleted = !followRelationship.IsDeleted;
            _unitOfWork.FollowerRepository.UpdateAsync(followRelationship);
            await _unitOfWork.SaveAsync();

            var message = followRelationship.IsDeleted ? MessageConstants.UNFOLLOW_USER_SUCCESS : MessageConstants.FOLLOW_USER_SUCCESS;

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = message,
                Data = _mapper.Map<FollowerModel>(followRelationship)
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
