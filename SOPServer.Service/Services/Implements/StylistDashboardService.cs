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

            // Build statistics model
            var statistics = BuildStatisticsModel(collections, targetYear, targetMonth, filter.TopCollectionsCount);

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
    }
}
