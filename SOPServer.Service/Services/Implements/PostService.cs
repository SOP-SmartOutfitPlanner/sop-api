using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
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
        private readonly NewsfeedSettings _newsfeedSettings;
        private readonly IRedisService _redisService;

        public PostService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IOptions<NewsfeedSettings> newsfeedSettings,
            IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _newsfeedSettings = newsfeedSettings.Value;
            _redisService = redisService;
        }

        public async Task<BaseResponseModel> CreatePostAsync(PostCreateModel model)
        {
            await ValidateUserExistsAsync(model.UserId);

            var newPost = await CreatePostEntityAsync(model);
            await AddPostImagesAsync(newPost.Id, model.ImageUrls);
            await HandlePostHashtagsAsync(newPost.Id, model.Hashtags);

            var createdPost = await GetPostWithRelationsAsync(newPost.Id);
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
            await _unitOfWork.SaveAsync();

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
                    .Include(p => p.CommentPosts)
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
            await ValidateUserExistsAsync(userId);

            // Generate sessionId if not provided
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N");
            }

            var cacheKey = RedisKeyConstants.GetNewsfeedCacheKey(userId, sessionId);
            
            // Try to get cached data
            var cachedData = await _redisService.GetAsync<NewsfeedCacheModel>(cacheKey);
            
            List<RankedPost> allRankedPosts;
            int totalCount;

            if (cachedData != null)
            {
                // Use cached data
                allRankedPosts = cachedData.RankedPosts;
                totalCount = cachedData.TotalCount;
            }
            else
            {
                // Fetch and rank posts
                var followedUserIds = await GetFollowedUserIdsAsync(userId);
                var currentTime = DateTime.UtcNow;
                var lookbackDate = currentTime.AddDays(-_newsfeedSettings.LookbackDays);

                var postsQuery = BuildNewsfeedQuery(followedUserIds, lookbackDate, currentTime);
                totalCount = await postsQuery.CountAsync();
                var posts = await postsQuery.ToListAsync();

                // Rank all posts (without pagination)
                allRankedPosts = RankPosts(
                    posts, 
                    sessionId,
                    _newsfeedSettings.RecencyWeight,
                    _newsfeedSettings.EngagementWeight,
                    _newsfeedSettings.RecencyWindowHour,
                    _newsfeedSettings.CommentMultiplier);

                // Cache the ranked posts for 30 minutes
                var cacheModel = new NewsfeedCacheModel
                {
                    RankedPosts = allRankedPosts,
                    TotalCount = totalCount,
                    CachedAt = DateTime.UtcNow
                };
   
                await _redisService.SetAsync(cacheKey, cacheModel, TimeSpan.FromMinutes(30));
            }

            // Paginate from cached/ranked posts
            var pagedPosts = allRankedPosts
                .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
                .Take(paginationParameter.PageSize)
                .ToList();

            var postIds = pagedPosts.Select(x => x.Post.PostId).ToList();
            var likedPostIds = await GetLikedPostIdsByUserAsync(userId, postIds);

            var feedModels = BuildNewsfeedModels(pagedPosts, likedPostIds);
            var pagination = CreatePagination(feedModels, totalCount, paginationParameter);

            return CreateNewsfeedResponse(pagination, totalCount, sessionId);
        }



        public async Task<BaseResponseModel> GetPostByUserIdAsync(PaginationParameter paginationParameter, long userId)
        {
            await ValidateUserExistsAsync(userId);

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

            var postModels = _mapper.Map<Pagination<PostModel>>(posts);

            return CreatePaginatedResponse(postModels, MessageConstants.GET_LIST_POST_BY_USER_SUCCESS);
        }

        #region Private Helper Methods

        private async Task ValidateUserExistsAsync(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }
        }

        private async Task<Post> CreatePostEntityAsync(PostCreateModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            
            var newPost = new Post
            {
                User = user,
                UserId = model.UserId,
                Body = model.Body
            };

            await _unitOfWork.PostRepository.AddAsync(newPost);
            await _unitOfWork.SaveAsync();

            return newPost;
        }

        private async Task AddPostImagesAsync(long postId, List<string> imageUrls)
        {
            if (imageUrls == null || !imageUrls.Any())
            {
                return;
            }

            foreach (var imageUrl in imageUrls)
            {
                var imagePost = new PostImage
                {
                    PostId = postId,
                    ImgUrl = imageUrl
                };
                await _unitOfWork.PostImageRepository.AddAsync(imagePost);
            }

            await _unitOfWork.SaveAsync();
        }

        private async Task HandlePostHashtagsAsync(long postId, List<string> hashtags)
        {
            if (hashtags == null || !hashtags.Any())
            {
                return;
            }

            var postHashtagsList = new List<PostHashtags>();

            foreach (var hashtagName in hashtags)
            {
                if (string.IsNullOrWhiteSpace(hashtagName))
                {
                    continue;
                }

                var hashtag = await GetOrCreateHashtagAsync(hashtagName.Trim());
                
                var postHashtag = new PostHashtags
                {
                    PostId = postId,
                    HashtagId = hashtag.Id
                };
                postHashtagsList.Add(postHashtag);
            }

            if (postHashtagsList.Any())
            {
                await _unitOfWork.PostHashtagsRepository.AddRangeAsync(postHashtagsList);
                await _unitOfWork.SaveAsync();
            }
        }

        private async Task<Hashtag> GetOrCreateHashtagAsync(string hashtagName)
        {
            var existingHashtag = await _unitOfWork.HashtagRepository.GetByNameAsync(hashtagName);
            
            if (existingHashtag != null)
            {
                return existingHashtag;
            }

            var newHashtag = new Hashtag
            {
                Name = hashtagName
            };
            
            await _unitOfWork.HashtagRepository.AddAsync(newHashtag);
            await _unitOfWork.SaveAsync();

            return newHashtag;
        }

        private async Task<Post?> GetPostWithRelationsAsync(long postId)
        {
            return await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                postId,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
            );
        }

        private async Task<List<long>> GetFollowedUserIdsAsync(long userId)
        {
            var followedUserIds = await _unitOfWork.FollowerRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(f => f.FollowerId == userId && !f.IsDeleted)
                .Select(f => f.FollowingId)
                .ToListAsync();

            followedUserIds.Add(userId);
            
            return followedUserIds;
        }

        private double CalculateRecencyScore(int hoursSinceCreation, int recencyWindowHours)
        {
            return Math.Max(0, Math.Min(1, 
                (recencyWindowHours - hoursSinceCreation) / (double)recencyWindowHours));
        }

        private double CalculateEngagementScore(int likeCount, int commentCount, int commentMultiplier)
        {
            var rawEngagement = likeCount + (commentCount * commentMultiplier);
            return Math.Log(1 + rawEngagement);
        }

        private double CalculateRankingScore(
            int hoursSinceCreation, 
            int likeCount, 
            int commentCount,
            long postId,
            string? sessionId,
            double recencyWeight,
            double engagementWeight,
            int recencyWindowHours,
            int commentMultiplier)
        {
            var recencyScore = CalculateRecencyScore(hoursSinceCreation, recencyWindowHours);
            var normalizedEngagement = CalculateEngagementScore(likeCount, commentCount, commentMultiplier);
            
            var rankingScore = (recencyWeight * recencyScore) + (engagementWeight * normalizedEngagement);
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                var hashCode = (sessionId + postId).GetHashCode();
                var shuffleFactor = (hashCode % 100) / 10000.0;
                rankingScore += shuffleFactor;
            }
            
            return rankingScore;
        }

        private async Task<HashSet<long>> GetLikedPostIdsByUserAsync(long userId, List<long> postIds)
        {
            var likedPostIdsList = await _unitOfWork.LikePostRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(lp => lp.UserId == userId && postIds.Contains(lp.PostId) && !lp.IsDeleted)
                .Select(lp => lp.PostId)
                .ToListAsync();
                
            return likedPostIdsList.ToHashSet();
        }

        private IQueryable<PostProjection> BuildNewsfeedQuery(List<long> followedUserIds, DateTime lookbackDate, DateTime currentTime)
        {
            return _unitOfWork.PostRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(p => !p.IsDeleted 
                    && p.UserId.HasValue
                    && followedUserIds.Contains(p.UserId.Value)
                    && p.CreatedDate >= lookbackDate)
                .Select(p => new PostProjection
                {
                    PostId = p.Id,
                    UserId = p.UserId ?? 0,
                    Body = p.Body,
                    CreatedDate = p.CreatedDate,
                    UpdatedDate = p.UpdatedDate,
                    UserDisplayName = p.User != null ? p.User.DisplayName : "Unknown",
                    AuthorAvatarUrl = p.User != null ? p.User.AvtUrl : null,
                    LikeCount = p.LikePosts.Count(lp => !lp.IsDeleted),
                    CommentCount = p.CommentPosts.Count(cp => !cp.IsDeleted),
                    Images = p.PostImages.Select(pi => pi.ImgUrl).ToList(),
                    Hashtags = p.PostHashtags.Select(ph => ph.Hashtag != null ? ph.Hashtag.Name : "").ToList(),
                    HoursSinceCreation = EF.Functions.DateDiffHour(p.CreatedDate, currentTime)
                });
        }

        private List<RankedPost> RankAndPaginatePosts(
            List<PostProjection> posts,
            PaginationParameter paginationParameter,
            string? sessionId,
            double recencyWeight,
            double engagementWeight,
            int recencyWindowHours,
            int commentMultiplier)
        {
            return posts.Select(p =>
            {
                var rankingScore = CalculateRankingScore(
                    p.HoursSinceCreation,
                    p.LikeCount,
                    p.CommentCount,
                    p.PostId,
                    sessionId,
                    recencyWeight,
                    engagementWeight,
                    recencyWindowHours,
                    commentMultiplier);

                return new RankedPost
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
        }

        private List<RankedPost> RankPosts(
            List<PostProjection> posts,
            string sessionId,
            double recencyWeight,
            double engagementWeight,
            int recencyWindowHours,
            int commentMultiplier)
        {
            return posts.Select(p =>
            {
                var rankingScore = CalculateRankingScore(
                    p.HoursSinceCreation,
                    p.LikeCount,
                    p.CommentCount,
                    p.PostId,
                    sessionId,
                    recencyWeight,
                    engagementWeight,
                    recencyWindowHours,
                    commentMultiplier);

                return new RankedPost
                {
                    Post = p,
                    RankingScore = rankingScore
                };
            })
            .OrderByDescending(x => x.RankingScore)
            .ThenByDescending(x => x.Post.CreatedDate)
            .ToList();
        }

        private List<NewsfeedPostModel> BuildNewsfeedModels(List<RankedPost> rankedPosts, HashSet<long> likedPostIds)
        {
            return rankedPosts.Select(x =>
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
        }
        public async Task<BaseResponseModel> GetAllPostsAsync(PaginationParameter paginationParameter)
        {
            var pagedPosts = await _unitOfWork.PostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag),
                orderBy: query => query.OrderByDescending(p => p.CreatedDate)
            );

            var postModels = _mapper.Map<Pagination<PostModel>>(pagedPosts);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_POST_SUCCESS,
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

        public async Task<BaseResponseModel> GetPostByIdAsync(long id)
        {
            var post = await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
            );

        private Pagination<NewsfeedPostModel> CreatePagination(
            List<NewsfeedPostModel> feedModels,
            int totalCount,
            PaginationParameter paginationParameter)
        {
            return new Pagination<NewsfeedPostModel>(
                feedModels,
                totalCount,
                paginationParameter.PageIndex,
                paginationParameter.PageSize);
        }

        private BaseResponseModel CreateNewsfeedResponse(Pagination<NewsfeedPostModel> pagination, int totalCount, string sessionId)
        {
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
                        pagination.HasPrevious,
                        SessionId = sessionId
                    }
                }
            };
        }

        private BaseResponseModel CreatePaginatedResponse<T>(Pagination<T> pagination, string message)
        {
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = message,
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

        #endregion
    }
}
