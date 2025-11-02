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
                .AsNoTracking()
                .Where(f => f.FollowerId == userId && !f.IsDeleted)
                .Select(f => f.FollowingId)
                .ToListAsync();

            // Include user's own posts
            followedUserIds.Add(userId);

            // Use single DateTime reference for consistency
            var currentTime = DateTime.UtcNow;
            var lookbackDate = currentTime.AddDays(-30);
            
            // Ranking constants
            const double RECENCY_WEIGHT = 0.4;
            const double ENGAGEMENT_WEIGHT = 0.6;
            const int RECENCY_WINDOW_HOURS = 72;
            const int COMMENT_MULTIPLIER = 2;

            // Query posts with optimized projection - no heavy Includes
            var postsQuery = _unitOfWork.PostRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(p => !p.IsDeleted 
                    && p.UserId.HasValue
                    && followedUserIds.Contains(p.UserId.Value)
                    && p.CreatedDate >= lookbackDate)
                .Select(p => new
                {
                    // Post info
                    PostId = p.Id,
                    UserId = p.UserId ?? 0,
                    Body = p.Body,
                    CreatedDate = p.CreatedDate,
                    UpdatedDate = p.UpdatedDate,
                    
                    // User info
                    UserDisplayName = p.User != null ? p.User.DisplayName : "Unknown",
                    AuthorAvatarUrl = p.User != null ? p.User.AvtUrl : null,
                    
                    // Summary data only - no full details
                    LikeCount = p.LikePosts.Count(lp => !lp.IsDeleted),
                    CommentCount = p.CommentPosts.Count(cp => !cp.IsDeleted),
                    
                    // Images
                    Images = p.PostImages.Select(pi => pi.ImgUrl).ToList(),
                    
                    // Hashtags
                    Hashtags = p.PostHashtags.Select(ph => ph.Hashtag != null ? ph.Hashtag.Name : "").ToList(),
                    
                    // Calculate hours difference for ranking
                    HoursSinceCreation = EF.Functions.DateDiffHour(p.CreatedDate, currentTime)
                });

            // Get total count for pagination
            var totalCount = await postsQuery.CountAsync();

            // Materialize the query to apply ranking in memory
            var posts = await postsQuery.ToListAsync();

            // Calculate ranking scores with clamping and normalization
            var rankedPosts = posts.Select(p =>
            {
                // Clamp recency score between 0 and 1 to prevent negative values
                var hoursSinceCreation = p.HoursSinceCreation;
                var recencyScore = Math.Max(0, Math.Min(1, 
                    (RECENCY_WINDOW_HOURS - hoursSinceCreation) / (double)RECENCY_WINDOW_HOURS));
                
                // Normalize engagement score using log-scale to reduce dominance of viral posts
                // Using log(1 + engagement) to handle zero engagement gracefully
                var rawEngagement = p.LikeCount + (p.CommentCount * COMMENT_MULTIPLIER);
                var normalizedEngagement = Math.Log(1 + rawEngagement);
                
                // Combined ranking score
                var rankingScore = (RECENCY_WEIGHT * recencyScore) + (ENGAGEMENT_WEIGHT * normalizedEngagement);
                
                // Add deterministic shuffle based on sessionId if provided
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Use hash of sessionId + postId for deterministic but unique ordering per session
                    var hashCode = (sessionId + p.PostId).GetHashCode();
                    var shuffleFactor = (hashCode % 100) / 10000.0; // Small adjustment: ±0.01
                    rankingScore += shuffleFactor;
                }
                
                return new
                {
                    Post = p,
                    RankingScore = rankingScore
                };
            })
            .OrderByDescending(x => x.RankingScore)
            .ThenByDescending(x => x.Post.CreatedDate)
            .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
            .Take(paginationParameter.PageSize)
            .ToList();

            // Get user's liked post IDs for this batch
            var postIds = rankedPosts.Select(x => x.Post.PostId).ToList();
            var likedPostIdsList = await _unitOfWork.LikePostRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(lp => lp.UserId == userId && postIds.Contains(lp.PostId) && !lp.IsDeleted)
                .Select(lp => lp.PostId)
                .ToListAsync();
            var likedPostIds = likedPostIdsList.ToHashSet();

            // Build response with compact essential data
            var feedModels = rankedPosts.Select(x =>
            {
                var p = x.Post;
                return new NewsfeedPostModel
                {
                    Id = p.PostId,
                    UserId = p.UserId,
                    UserDisplayName = p.UserDisplayName,
                    Body = p.Body,
                    CreatedAt = p.CreatedDate,
                    UpdatedAt = p.UpdatedDate,
                    Hashtags = p.Hashtags.Where(h => !string.IsNullOrEmpty(h)).ToList(),
                    Images = p.Images.Where(i => !string.IsNullOrEmpty(i)).ToList(),
                    LikeCount = p.LikeCount,
                    CommentCount = p.CommentCount,
                    IsLikedByUser = likedPostIds.Contains(p.PostId),
                    AuthorAvatarUrl = p.AuthorAvatarUrl,
                    RankingScore = x.RankingScore
                };
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
