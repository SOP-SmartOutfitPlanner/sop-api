using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PostService(
            IUnitOfWork unitOfWork, 
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> CreatePostAsync(PostCreateModel model)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Create new post
            var newPost = new Post
            {
                User = user,
                UserId = model.UserId,
                Body = model.Body
            };

            await _unitOfWork.PostRepository.AddAsync(newPost);
            _unitOfWork.Save();

            foreach (var imageUrl in model.ImageUrls)
            {
                var imagePost = new PostImage
                {
                    PostId = newPost.Id,
                    ImgUrl = imageUrl
                };
                await _unitOfWork.PostImageRepository.AddAsync(imagePost);
            }
            _unitOfWork.Save();


            // Handle hashtags
            if (model.Hashtags != null && model.Hashtags.Any())
            {
                var postHashtagsList = new List<PostHashtags>();

                foreach (var hashtagName in model.Hashtags)
                {
                    if (string.IsNullOrWhiteSpace(hashtagName))
                        continue;

                    // Check if hashtag exists
                    var existingHashtag = await _unitOfWork.HashtagRepository.GetByNameAsync(hashtagName.Trim());
                    
                    Hashtag hashtag;
                    if (existingHashtag == null)
                    {
                        // Create new hashtag
                        hashtag = new Hashtag
                        {
                            Name = hashtagName.Trim()
                        };
                        await _unitOfWork.HashtagRepository.AddAsync(hashtag);
                        _unitOfWork.Save();
                    }
                    else
                    {
                        hashtag = existingHashtag;
                    }

                    // Create post-hashtag relationship
                    var postHashtag = new PostHashtags
                    {
                        PostId = newPost.Id,
                        HashtagId = hashtag.Id
                    };
                    postHashtagsList.Add(postHashtag);
                }

                if (postHashtagsList.Any())
                {
                    await _unitOfWork.PostHashtagsRepository.AddRangeAsync(postHashtagsList);
                    _unitOfWork.Save();
                }
            }

            // Retrieve the created post with all related data
            var createdPost = await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                newPost.Id,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
            );

            var postModel = _mapper.Map<PostModel>(createdPost);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.POST_CREATE_SUCCESS,
                Data = postModel
            };
        }

        public async Task<BaseResponseModel> DeletePostByIdAsync(long id)
        {
            var post = await _unitOfWork.PostRepository.GetByIdAsync(id);

            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            _unitOfWork.PostRepository.SoftDeleteAsync(post);
            _unitOfWork.Save();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.POST_DELETE_SUCCESS
            };
        }

        public async Task<BaseResponseModel> GetPostByIdAsync(long id)
        {
            var post = await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
            );

            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            var postModel = _mapper.Map<PostModel>(post);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.POST_GET_SUCCESS,
                Data = postModel
            };
        }

        public async Task<BaseResponseModel> GetNewsFeedAsync(
            PaginationParameter paginationParameter, 
            long userId, 
            string? sessionId = null)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Get list of users that the current user follows
            var followedUserIds = await _unitOfWork.FollowerRepository
                .GetQueryable()
                .Where(f => f.FollowerId == userId && !f.IsDeleted)
                .Select(f => f.FollowingId)
                .ToListAsync();

            // Include user's own posts
            followedUserIds.Add(userId);

            // Get posts from followed users within the last 30 days
            var lookbackDate = DateTime.UtcNow.AddDays(-30);
            
            // Query posts with ranking score calculation
            var postsQuery = _unitOfWork.PostRepository
                .GetQueryable()
                .Where(p => !p.IsDeleted 
                    && p.UserId.HasValue
                    && followedUserIds.Contains(p.UserId.Value)
                    && p.CreatedDate >= lookbackDate)
                .Include(p => p.User)
                .Include(p => p.PostImages)
                .Include(p => p.PostHashtags)
                    .ThenInclude(ph => ph.Hashtag)
                .Include(p => p.LikePosts)
                .Include(p => p.CommentPosts)
                .Select(p => new
                {
                    Post = p,
                    // Simple ranking score: combine recency and engagement
                    // Recency: hours since creation (newer = higher score)
                    RecencyScore = (72 - EF.Functions.DateDiffHour(p.CreatedDate, DateTime.UtcNow)) / 72.0,
                    // Engagement: likes + (comments * 2) - comments are valued more
                    EngagementScore = p.LikePosts.Count(lp => !lp.IsDeleted) + 
                                    (p.CommentPosts.Count(cp => !cp.IsDeleted) * 2),
                    // Combined ranking score
                    RankingScore = 
                        // Recency weight (40%): favor recent posts within 3 days
                        (0.4 * ((72 - EF.Functions.DateDiffHour(p.CreatedDate, DateTime.UtcNow)) / 72.0)) +
                        // Engagement weight (60%): favor posts with more interactions
                        (0.6 * (p.LikePosts.Count(lp => !lp.IsDeleted) + 
                               (p.CommentPosts.Count(cp => !cp.IsDeleted) * 2)))
                });

            // Get total count for pagination
            var totalCount = await postsQuery.CountAsync();

            // Apply sorting by ranking score and pagination
            var rankedPosts = await postsQuery
                .OrderByDescending(x => x.RankingScore)
                .ThenByDescending(x => x.Post.CreatedDate)
                .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
                .Take(paginationParameter.PageSize)
                .ToListAsync();

            // Get user's liked post IDs for this batch
            var postIds = rankedPosts.Select(x => x.Post.Id).ToList();
            var likedPostIdsList = await _unitOfWork.LikePostRepository
                .GetQueryable()
                .Where(lp => lp.UserId == userId && postIds.Contains(lp.PostId) && !lp.IsDeleted)
                .Select(lp => lp.PostId)
                .ToListAsync();
            var likedPostIds = likedPostIdsList.ToHashSet();

            // Build response with engagement data
            var feedModels = rankedPosts.Select(x =>
            {
                var model = _mapper.Map<NewsfeedPostModel>(x.Post);
                model.RankingScore = x.RankingScore;
                model.IsLikedByUser = likedPostIds.Contains(x.Post.Id);
                return model;
            }).ToList();

            var pagination = new Pagination<NewsfeedPostModel>(
                feedModels,
                totalCount,
                paginationParameter.PageIndex,
                paginationParameter.PageSize);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = totalCount > 0 ? MessageConstants.NEWSFEED_GET_SUCCESS : MessageConstants.NEWSFEED_EMPTY,
                Data = new ModelPaging
                {
                    Data = pagination,
                    MetaData = new
                    {
                        pagination.TotalCount,
                        pagination.PageSize,
                        pagination.CurrentPage,
                        pagination.TotalPages,
                        pagination.HasNext,
                        pagination.HasPrevious
                    }
                }
            };
        }



        public async Task<BaseResponseModel> GetPostByUserIdAsync(PaginationParameter paginationParameter, long userId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Get posts by userId with pagination and all related data
            var posts = await _unitOfWork.PostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts),
                filter: p => p.UserId == userId,
                orderBy: q => q.OrderByDescending(p => p.CreatedDate)
            );

            // Map to PostModel
            var postModels = _mapper.Map<Pagination<PostModel>>(posts);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_POST_BY_USER_SUCCESS,
                Data = new ModelPaging
                {
                    Data = postModels,
                    MetaData = new
                    {
                        postModels.TotalCount,
                        postModels.PageSize,
                        postModels.CurrentPage,
                        postModels.TotalPages,
                        postModels.HasNext,
                        postModels.HasPrevious
                    }
                }
            };
        }
    }
}
