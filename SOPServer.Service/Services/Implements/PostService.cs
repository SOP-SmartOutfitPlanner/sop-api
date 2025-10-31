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
using SOPServer.Service.Utils;
using StackExchange.Redis;
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
        private readonly NewsfeedRedisHelper _redisHelper;
        private readonly Random _random;

        public PostService(
            IUnitOfWork unitOfWork, 
            IMapper mapper,
            IOptions<NewsfeedSettings> newsfeedSettings,
            IConnectionMultiplexer redis)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _newsfeedSettings = newsfeedSettings.Value;
            _redisHelper = new NewsfeedRedisHelper(redis);
            _random = new Random();
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

            // Generate session ID if not provided
            sessionId ??= Guid.NewGuid().ToString("N");

            // Step 1: Get or build candidate set
            var candidates = await GetOrBuildCandidateSetAsync(userId);

            if (!candidates.Any())
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.NEWSFEED_EMPTY,
                    Data = new ModelPaging
                    {
                        Data = new Pagination<NewsfeedPostModel>(
                            new List<NewsfeedPostModel>(), 
                            0, 
                            paginationParameter.PageIndex, 
                            paginationParameter.PageSize),
                        MetaData = new
                        {
                            TotalCount = 0,
                            PageSize = paginationParameter.PageSize,
                            CurrentPage = paginationParameter.PageIndex,
                            TotalPages = 0,
                            HasNext = false,
                            HasPrevious = false
                        }
                    }
                };
            }

            // Step 2: Get seen posts to exclude
            var seenPosts = await _redisHelper.GetSeenPostsAsync(userId, sessionId);

            // Step 3: Filter out seen posts
            var unseenCandidates = candidates
                .Where(kvp => !seenPosts.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Step 4: Re-rank with current time-decay, jitter, and diversity
            var rankedPosts = await RankPostsAsync(userId, unseenCandidates);

            // Step 5: Apply pagination
            var totalCount = rankedPosts.Count;
            var skip = (paginationParameter.PageIndex - 1) * paginationParameter.PageSize;
            var pagedPostIds = rankedPosts
                .Skip(skip)
                .Take(paginationParameter.PageSize)
                .ToList();

            // Step 6: Fetch full post data
            var posts = await FetchPostDetailsAsync(pagedPostIds.Select(p => p.PostId).ToList(), userId);

            // Step 7: Get user's liked post IDs for this batch
            var postIds = posts.Select(p => p.Id).ToList();
            var likedPostIdsList = await _unitOfWork.LikePostRepository
                .GetQueryable()
                .Where(lp => lp.UserId == userId && postIds.Contains(lp.PostId) && !lp.IsDeleted)
                .Select(lp => lp.PostId)
                .ToListAsync();
            var likedPostIds = likedPostIdsList.ToHashSet();

            // Step 8: Mark as seen
            await _redisHelper.AddSeenPostsAsync(
                userId, 
                sessionId, 
                pagedPostIds.Select(p => p.PostId),
                TimeSpan.FromMinutes(_newsfeedSettings.SeenPostsTTL));

            // Step 9: Build response with engagement data
            var feedModels = posts.Select(p =>
            {
                var model = _mapper.Map<NewsfeedPostModel>(p);
                var rankedPost = pagedPostIds.FirstOrDefault(rp => rp.PostId == p.Id);
                if (rankedPost.PostId != 0)
                {
                    model.RankingScore = rankedPost.Score;
                }
                model.IsLikedByUser = likedPostIds.Contains(p.Id);
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
                Message = MessageConstants.NEWSFEED_GET_SUCCESS,
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

        /// <summary>
        /// Gets candidate set from Redis cache or builds it from database.
        /// Candidates include: posts from followed users + user's own posts + trending backfill.
        /// </summary>
        private async Task<Dictionary<long, double>> GetOrBuildCandidateSetAsync(long userId)
        {
            // Check cache first
            if (await _redisHelper.CandidatesExistAsync(userId))
            {
                return await _redisHelper.GetCandidatesAsync(userId);
            }

            // Build candidate set from database
            var candidates = new Dictionary<long, double>();

            // Get followed user IDs
            var followedUserIds = await _unitOfWork.FollowerRepository
                .GetQueryable()
                .Where(f => f.FollowerId == userId && !f.IsDeleted)
                .Select(f => f.FollowingId)
                .ToListAsync();

            // Include user's own ID
            followedUserIds.Add(userId);

            // Fetch posts from followed users within lookback window
            var lookbackDate = DateTime.UtcNow.AddDays(-_newsfeedSettings.CandidateLookbackDays);
            var posts = await _unitOfWork.PostRepository
                .GetQueryable()
                .Where(p => !p.IsDeleted 
                    && p.UserId.HasValue
                    && followedUserIds.Contains(p.UserId.Value)
                    && p.CreatedDate >= lookbackDate)
                .Include(p => p.LikePosts)
                .Include(p => p.CommentPosts)
                .OrderByDescending(p => p.CreatedDate)
                .Take(_newsfeedSettings.MaxCandidateFetch)
                .ToListAsync();

            // Calculate base scores and cache metrics
            foreach (var post in posts)
            {
                var baseScore = CalculateBaseScore(post);
                candidates[post.Id] = baseScore;

                // Cache post metrics
                await _redisHelper.SetPostMetricsAsync(
                    post.Id,
                    new PostMetricsCache
                    {
                        Likes = post.LikePosts?.Count(lp => !lp.IsDeleted) ?? 0,
                        Comments = post.CommentPosts?.Count(cp => !cp.IsDeleted) ?? 0,
                        Reshares = 0,
                        AuthorId = post.UserId ?? 0,
                        CreatedAt = post.CreatedDate
                    },
                    TimeSpan.FromMinutes(_newsfeedSettings.MetricsCacheTTL));
            }

            // Backfill with trending posts if candidate pool is small
            if (candidates.Count < _newsfeedSettings.MinCandidates)
            {
                var trendingPosts = await GetTrendingPostsAsync(
                    _newsfeedSettings.MinCandidates - candidates.Count,
                    candidates.Keys.ToHashSet());

                foreach (var post in trendingPosts)
                {
                    var baseScore = CalculateBaseScore(post);
                    candidates[post.Id] = baseScore;

                    await _redisHelper.SetPostMetricsAsync(
                        post.Id,
                        new PostMetricsCache
                        {
                            Likes = post.LikePosts?.Count(lp => !lp.IsDeleted) ?? 0,
                            Comments = post.CommentPosts?.Count(cp => !cp.IsDeleted) ?? 0,
                            Reshares = 0,
                            AuthorId = post.UserId ?? 0,
                            CreatedAt = post.CreatedDate
                        },
                        TimeSpan.FromMinutes(_newsfeedSettings.MetricsCacheTTL));
                }
            }

            // Cache candidates
            await _redisHelper.SetCandidatesAsync(
                userId,
                candidates,
                TimeSpan.FromMinutes(_newsfeedSettings.CandidateCacheTTL));

            return candidates;
        }

        /// <summary>
        /// Calculates base score for a post (without user-specific factors).
        /// Used for initial candidate scoring and caching.
        /// </summary>
        private double CalculateBaseScore(Post post)
        {
            var likes = post.LikePosts?.Count(lp => !lp.IsDeleted) ?? 0;
            var comments = post.CommentPosts?.Count(cp => !cp.IsDeleted) ?? 0;
            var reshares = 0;

            var recency = NewsfeedScoringUtils.CalculateRecencyScore(
                post.CreatedDate,
                _newsfeedSettings.Lambda);

            var engagement = NewsfeedScoringUtils.CalculateEngagementScore(
                likes,
                comments,
                reshares,
                _newsfeedSettings.Alpha,
                _newsfeedSettings.Beta,
                _newsfeedSettings.Gamma);

            // Base score = weighted recency + engagement
            return (_newsfeedSettings.Wr * recency) + (_newsfeedSettings.We * engagement);
        }

        /// <summary>
        /// Re-ranks posts with user-specific factors, time-decay, jitter, and diversity.
        /// Implements Facebook-like refresh dynamics.
        /// </summary>
        private async Task<List<(long PostId, double Score)>> RankPostsAsync(
            long userId,
            Dictionary<long, double> candidates)
        {
            var scored = new List<(long PostId, double Score, long AuthorId)>();

            // Clear author counts for fresh diversity calculation
            await _redisHelper.ClearAuthorCountsAsync(userId);

            foreach (var candidate in candidates)
            {
                var postId = candidate.Key;
                var baseScore = candidate.Value;

                // Get cached metrics
                var metrics = await _redisHelper.GetPostMetricsAsync(postId);
                if (metrics == null) continue;

                // Recalculate time-decay with current time
                var recency = NewsfeedScoringUtils.CalculateRecencyScore(
                    metrics.CreatedAt,
                    _newsfeedSettings.Lambda);

                var engagement = NewsfeedScoringUtils.CalculateEngagementScore(
                    metrics.Likes,
                    metrics.Comments,
                    metrics.Reshares,
                    _newsfeedSettings.Alpha,
                    _newsfeedSettings.Beta,
                    _newsfeedSettings.Gamma);

                // Calculate affinity (simplified - in production, query user-author interaction history)
                var affinity = await CalculateAffinityAsync(userId, metrics.AuthorId);

                // Calculate author quality (simplified - in production, use EMA of engagement rate)
                var quality = 0.5; // Placeholder

                // Calculate diversity penalty
                var authorPostCount = await _redisHelper.GetAuthorCountAsync(userId, metrics.AuthorId);
                var diversity = NewsfeedScoringUtils.CalculateDiversityPenalty(
                    authorPostCount,
                    _newsfeedSettings.DiversityThreshold,
                    _newsfeedSettings.Delta);

                // Calculate negative feedback (simplified - in production, track user feedback)
                var negativeFeedback = 0.0; // Placeholder

                // Calculate contextual boost (simplified)
                var contextualBoost = 0.0; // Placeholder

                // Composite score
                var score = NewsfeedScoringUtils.CalculateCompositeScore(
                    recency,
                    engagement,
                    affinity,
                    quality,
                    diversity,
                    negativeFeedback,
                    contextualBoost,
                    _newsfeedSettings.Wr,
                    _newsfeedSettings.We,
                    _newsfeedSettings.Wa,
                    _newsfeedSettings.Wc,
                    _newsfeedSettings.Wd,
                    _newsfeedSettings.Wn,
                    _newsfeedSettings.Wb);

                // Apply jitter for variance
                score = NewsfeedScoringUtils.ApplyJitter(
                    score,
                    _newsfeedSettings.JitterPercent,
                    _random);

                scored.Add((postId, score, metrics.AuthorId));

                // Track author count for diversity
                await _redisHelper.IncrementAuthorCountAsync(userId, metrics.AuthorId);
            }

            // Sort by score descending
            var ranked = scored
                .OrderByDescending(s => s.Score)
                .Select(s => (s.PostId, s.Score))
                .ToList();

            // Apply ε-greedy exploration (inject trending posts)
            if (_random.NextDouble() < _newsfeedSettings.ExploreRate && ranked.Count > 10)
            {
                // Swap a few top posts with lower-ranked posts for exploration
                var exploreCount = Math.Max(1, (int)(ranked.Count * _newsfeedSettings.ExploreRate));
                for (int i = 0; i < exploreCount && i < ranked.Count / 2; i++)
                {
                    var topIndex = _random.Next(0, ranked.Count / 3);
                    var exploreIndex = _random.Next(ranked.Count / 2, ranked.Count);
                    (ranked[topIndex], ranked[exploreIndex]) = (ranked[exploreIndex], ranked[topIndex]);
                }
            }

            return ranked;
        }

        /// <summary>
        /// Calculates affinity score between user and author.
        /// In production, this should query user-author interaction history.
        /// </summary>
        private async Task<double> CalculateAffinityAsync(long userId, long authorId)
        {
            if (userId == authorId)
                return 1.0; // Max affinity for own posts

            // Simplified: check if user follows author
            var isFollowing = await _unitOfWork.FollowerRepository
                .IsFollowing(userId, authorId);

            // Simplified: count past likes to author's posts
            var pastLikes = await _unitOfWork.LikePostRepository
                .GetQueryable()
                .Where(lp => lp.UserId == userId && !lp.IsDeleted)
                .Join(
                    _unitOfWork.PostRepository.GetQueryable(),
                    lp => lp.PostId,
                    p => p.Id,
                    (lp, p) => new { lp, p })
                .Where(x => x.p.UserId == authorId && !x.p.IsDeleted)
                .CountAsync();

            // Simplified: count past comments to author's posts
            var pastComments = await _unitOfWork.CommentPostRepository
                .GetQueryable()
                .Where(cp => cp.UserId == userId && !cp.IsDeleted)
                .Join(
                    _unitOfWork.PostRepository.GetQueryable(),
                    cp => cp.PostId,
                    p => p.Id,
                    (cp, p) => new { cp, p })
                .Where(x => x.p.UserId == authorId && !x.p.IsDeleted)
                .CountAsync();

            var affinity = NewsfeedScoringUtils.CalculateAffinityScore(
                pastLikes,
                pastComments,
                0, // directReplies placeholder
                0, // profileVisits placeholder
                _newsfeedSettings.W1,
                _newsfeedSettings.W2,
                _newsfeedSettings.W3,
                _newsfeedSettings.W4,
                _newsfeedSettings.MaxAffinity);

            // Boost if following
            if (isFollowing)
                affinity = Math.Min(affinity + 0.3, 1.0);

            return affinity;
        }

        /// <summary>
        /// Gets trending posts for backfill.
        /// Uses recent posts with high engagement.
        /// </summary>
        private async Task<List<Post>> GetTrendingPostsAsync(int count, HashSet<long> excludeIds)
        {
            var lookbackDate = DateTime.UtcNow.AddDays(-3); // Recent trending

            var trendingPosts = await _unitOfWork.PostRepository
                .GetQueryable()
                .Where(p => !p.IsDeleted 
                    && p.CreatedDate >= lookbackDate
                    && !excludeIds.Contains(p.Id))
                .Include(p => p.LikePosts)
                .Include(p => p.CommentPosts)
                .OrderByDescending(p => p.LikePosts.Count(lp => !lp.IsDeleted) 
                    + (p.CommentPosts.Count(cp => !cp.IsDeleted) * 2))
                .Take(count)
                .ToListAsync();

            return trendingPosts;
        }

        /// <summary>
        /// Fetches full post details with all related data for display.
        /// </summary>
        private async Task<List<Post>> FetchPostDetailsAsync(List<long> postIds, long userId)
        {
            if (!postIds.Any())
                return new List<Post>();

            var posts = await _unitOfWork.PostRepository
                .GetQueryable()
                .Where(p => postIds.Contains(p.Id) && !p.IsDeleted)
                .Include(p => p.User)
                .Include(p => p.PostImages)
                .Include(p => p.PostHashtags)
                    .ThenInclude(ph => ph.Hashtag)
                .Include(p => p.LikePosts)
                .Include(p => p.CommentPosts)
                .ToListAsync();

            // Maintain order from postIds
            var orderedPosts = postIds
                .Select(id => posts.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList();

            return orderedPosts;
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
