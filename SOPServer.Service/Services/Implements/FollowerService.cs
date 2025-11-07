using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
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

        public async Task<BaseResponseModel> GetFollowersByUserId(PaginationParameter paginationParameter, long userId)
        {
            // Check if user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Get followers with user details
            var followers = await _unitOfWork.FollowerRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query.Include(f => f.FollowerUser),
                filter: f => f.FollowingId == userId,
                orderBy: q => q.OrderByDescending(f => f.CreatedDate));

            // Manually map to FollowerUserModel to ensure correct navigation property is used
            var followerModels = followers.Select(f => new FollowerUserModel
            {
                Id = f.Id,
                UserId = f.FollowerId,
                DisplayName = f.FollowerUser?.DisplayName,
                AvatarUrl = f.FollowerUser?.AvtUrl,
                Bio = f.FollowerUser?.Bio,
                CreatedDate = f.CreatedDate
            }).ToList();

            var paginatedResult = new Pagination<FollowerUserModel>(
                followerModels,
                followers.TotalCount,
                followers.CurrentPage,
                followers.PageSize);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_FOLLOWERS_LIST_SUCCESS,
                Data = new ModelPaging
                {
                    Data = paginatedResult,
                    MetaData = new
                    {
                        paginatedResult.TotalCount,
                        paginatedResult.PageSize,
                        paginatedResult.CurrentPage,
                        paginatedResult.TotalPages,
                        paginatedResult.HasNext,
                        paginatedResult.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetFollowingByUserId(PaginationParameter paginationParameter, long userId)
        {
            // Check if user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Get following with user details
            var following = await _unitOfWork.FollowerRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query.Include(f => f.FollowingUser),
                filter: f => f.FollowerId == userId,
                orderBy: q => q.OrderByDescending(f => f.CreatedDate));

            // Manually map to FollowerUserModel to ensure correct navigation property is used
            var followingModels = following.Select(f => new FollowerUserModel
            {
                Id = f.Id,
                UserId = f.FollowingId,
                DisplayName = f.FollowingUser?.DisplayName,
                AvatarUrl = f.FollowingUser?.AvtUrl,
                Bio = f.FollowingUser?.Bio,
                CreatedDate = f.CreatedDate
            }).ToList();

            var paginatedResult = new Pagination<FollowerUserModel>(
                followingModels,
                following.TotalCount,
                following.CurrentPage,
                following.PageSize);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_FOLLOWING_LIST_SUCCESS,
                Data = new ModelPaging
                {
                    Data = paginatedResult,
                    MetaData = new
                    {
                        paginatedResult.TotalCount,
                        paginatedResult.PageSize,
                        paginatedResult.CurrentPage,
                        paginatedResult.TotalPages,
                        paginatedResult.HasNext,
                        paginatedResult.HasPrevious
                    }
                }
            };
        }
    }
}
