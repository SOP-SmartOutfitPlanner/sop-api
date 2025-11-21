using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.DashboardModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class StylistDashboardService : IStylistDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StylistDashboardService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetCollectionStatisticsByUserAsync(long userId, DashboardFilterModel filter)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Set defaults and validate filter
            var targetYear = filter.Year ?? DateTime.UtcNow.Year;
            var targetMonth = filter.Month;

            // Validate month if provided
            if (targetMonth.HasValue && (targetMonth.Value < 1 || targetMonth.Value > 12))
            {
                throw new BadRequestException("Month must be between 1 and 12");
            }

            // Validate topCollectionsCount
            if (filter.TopCollectionsCount < 1)
            {
                throw new BadRequestException("Top collections count must be at least 1");
            }

            // Get all user's collections with engagement data
            var collections = await _unitOfWork.CollectionRepository.GetQueryable()
                .Include(c => c.LikeCollections.Where(lc => !lc.IsDeleted))
                .Include(c => c.CommentCollections.Where(cc => !cc.IsDeleted))
                .Include(c => c.SaveCollections.Where(sc => !sc.IsDeleted))
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .ToListAsync();

            // Get follower statistics
            var totalFollowers = await _unitOfWork.FollowerRepository.GetFollowerCount(userId);
            
            // Calculate followers this month based on UpdatedDate or CreatedDate
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            
            var allFollowers = await _unitOfWork.FollowerRepository.GetQueryable()
                .Where(f => f.FollowingId == userId && !f.IsDeleted)
                .ToListAsync();
            
            var followersThisMonth = allFollowers.Count(f =>
            {
                var relevantDate = f.UpdatedDate ?? f.CreatedDate;
                return relevantDate.Month == currentMonth && relevantDate.Year == currentYear;
            });

            // Build statistics model
            var statistics = BuildStatisticsModel(collections, targetYear, targetMonth, filter.TopCollectionsCount);
            
            // Add follower statistics
            statistics.TotalFollowers = totalFollowers;
            statistics.FollowersThisMonth = followersThisMonth;

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_COLLECTION_STATISTICS_SUCCESS,
                Data = statistics
            };
        }

        private CollectionStatisticsModel BuildStatisticsModel(
            List<Repository.Entities.Collection> collections,
            int targetYear,
            int? targetMonth,
            int topCollectionsCount)
        {
            var statistics = new CollectionStatisticsModel
            {
                TotalCollections = collections.Count,
                PublishedCollections = collections.Count(c => c.IsPublished),
                UnpublishedCollections = collections.Count(c => !c.IsPublished),
                TotalLikes = collections.Sum(c => c.LikeCollections.Count),
                TotalComments = collections.Sum(c => c.CommentCollections.Count),
                TotalSaves = collections.Sum(c => c.SaveCollections.Count)
            };

            // Calculate monthly statistics
            statistics.MonthlyStats = targetMonth.HasValue
                ? new List<MonthlyStatisticsModel> { CalculateMonthlyStatistics(collections, targetYear, targetMonth.Value) }
                : Enumerable.Range(1, 12)
                    .Select(month => CalculateMonthlyStatistics(collections, targetYear, month))
                    .ToList();

            // Get top performing collections using AutoMapper
            statistics.TopCollections = collections
                .Select(c => _mapper.Map<TopCollectionModel>(c))
                .OrderByDescending(c => c.TotalEngagement)
                .ThenByDescending(c => c.CreatedDate)
                .Take(topCollectionsCount)
                .ToList();

            return statistics;
        }

        private MonthlyStatisticsModel CalculateMonthlyStatistics(
            List<Repository.Entities.Collection> collections,
            int year,
            int month)
        {
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            // Filter collections created in the specific month
            var collectionsInMonth = collections
                .Where(c => c.CreatedDate.Year == year && c.CreatedDate.Month == month)
                .ToList();

            // Calculate engagement metrics received in the specific month
            var likesInMonth = collections
                .SelectMany(c => c.LikeCollections)
                .Count(lc => lc.CreatedDate.Year == year && lc.CreatedDate.Month == month);

            var commentsInMonth = collections
                .SelectMany(c => c.CommentCollections)
                .Count(cc => cc.CreatedDate.Year == year && cc.CreatedDate.Month == month);

            var savesInMonth = collections
                .SelectMany(c => c.SaveCollections)
                .Count(sc => sc.CreatedDate.Year == year && sc.CreatedDate.Month == month);

            return new MonthlyStatisticsModel
            {
                Month = month,
                Year = year,
                MonthName = monthName,
                CollectionsCreated = collectionsInMonth.Count,
                LikesReceived = likesInMonth,
                CommentsReceived = commentsInMonth,
                SavesReceived = savesInMonth,
                TotalEngagement = likesInMonth + commentsInMonth + savesInMonth
            };
        }

        public async Task<BaseResponseModel> GetPostStatisticsByUserAsync(long userId, PostDashboardFilterModel filter)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Set defaults and validate filter
            var targetYear = filter.Year ?? DateTime.UtcNow.Year;
            var targetMonth = filter.Month;

            // Validate month if provided
            if (targetMonth.HasValue && (targetMonth.Value < 1 || targetMonth.Value > 12))
            {
                throw new BadRequestException("Month must be between 1 and 12");
            }

            // Validate topPostsCount
            if (filter.TopPostsCount < 1)
            {
                throw new BadRequestException("Top posts count must be at least 1");
            }

            // Get all user's posts with engagement data
            var posts = await _unitOfWork.PostRepository.GetQueryable()
                .Include(p => p.LikePosts.Where(lp => !lp.IsDeleted))
                .Include(p => p.CommentPosts.Where(cp => !cp.IsDeleted))
                .Include(p => p.PostImages.Where(pi => !pi.IsDeleted))
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .ToListAsync();

            // Get follower statistics
            var totalFollowers = await _unitOfWork.FollowerRepository.GetFollowerCount(userId);
            
            // Calculate followers this month based on UpdatedDate or CreatedDate
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            
            var allFollowers = await _unitOfWork.FollowerRepository.GetQueryable()
                .Where(f => f.FollowingId == userId && !f.IsDeleted)
                .ToListAsync();
            
            var followersThisMonth = allFollowers.Count(f =>
            {
                var relevantDate = f.UpdatedDate ?? f.CreatedDate;
                return relevantDate.Month == currentMonth && relevantDate.Year == currentYear;
            });

            // Build statistics model
            var statistics = BuildPostStatisticsModel(posts, targetYear, targetMonth, filter.TopPostsCount);
            
            // Add follower statistics
            statistics.TotalFollowers = totalFollowers;
            statistics.FollowersThisMonth = followersThisMonth;

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_POST_STATISTICS_SUCCESS,
                Data = statistics
            };
        }

        private PostStatisticsModel BuildPostStatisticsModel(
            List<Repository.Entities.Post> posts,
            int targetYear,
            int? targetMonth,
            int topPostsCount)
        {
            var statistics = new PostStatisticsModel
            {
                TotalPosts = posts.Count,
                TotalLikes = posts.Sum(p => p.LikePosts.Count),
                TotalComments = posts.Sum(p => p.CommentPosts.Count)
            };

            // Calculate monthly statistics
            statistics.MonthlyStats = targetMonth.HasValue
                ? new List<MonthlyPostStatisticsModel> { CalculateMonthlyPostStatistics(posts, targetYear, targetMonth.Value) }
                : Enumerable.Range(1, 12)
                    .Select(month => CalculateMonthlyPostStatistics(posts, targetYear, month))
                    .ToList();

            // Get top performing posts
            statistics.TopPosts = posts
                .Select(p => new TopPostModel
                {
                    Id = p.Id,
                    Body = p.Body ?? string.Empty,
                    Images = p.PostImages
                        .Where(pi => !pi.IsDeleted)
                        .Select(pi => pi.ImgUrl)
                        .ToList(),
                    LikeCount = p.LikePosts.Count,
                    CommentCount = p.CommentPosts.Count,
                    TotalEngagement = p.LikePosts.Count + p.CommentPosts.Count,
                    CreatedDate = p.CreatedDate
                })
                .OrderByDescending(p => p.TotalEngagement)
                .ThenByDescending(p => p.CreatedDate)
                .Take(topPostsCount)
                .ToList();

            return statistics;
        }

        private MonthlyPostStatisticsModel CalculateMonthlyPostStatistics(
            List<Repository.Entities.Post> posts,
            int year,
            int month)
        {
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            // Filter posts created in the specific month
            var postsInMonth = posts
                .Where(p => p.CreatedDate.Year == year && p.CreatedDate.Month == month)
                .ToList();

            // Calculate engagement metrics received in the specific month
            var likesInMonth = posts
                .SelectMany(p => p.LikePosts)
                .Count(lp => lp.CreatedDate.Year == year && lp.CreatedDate.Month == month);

            var commentsInMonth = posts
                .SelectMany(p => p.CommentPosts)
                .Count(cp => cp.CreatedDate.Year == year && cp.CreatedDate.Month == month);

            return new MonthlyPostStatisticsModel
            {
                Month = month,
                Year = year,
                MonthName = monthName,
                PostsCreated = postsInMonth.Count,
                LikesReceived = likesInMonth,
                CommentsReceived = commentsInMonth,
                TotalEngagement = likesInMonth + commentsInMonth
            };
        }
    }
}
