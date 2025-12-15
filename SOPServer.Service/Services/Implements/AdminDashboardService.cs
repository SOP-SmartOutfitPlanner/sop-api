using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.AdminDashboardModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Interfaces;
using System.Globalization;

namespace SOPServer.Service.Services.Implements
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AdminDashboardService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetRevenueStatisticsAsync(RevenueFilterModel filter)
        {
            var transactionsQuery = _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                .Include(t => t.UserSubscription)
                    .ThenInclude(us => us.User)
                .Include(t => t.UserSubscription)
                    .ThenInclude(us => us.SubscriptionPlan)
                .Where(t => !t.IsDeleted && t.UserSubscription.SubscriptionPlan.Price > 0);

            if (filter.Year.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(t => t.CreatedDate.Year == filter.Year.Value);
            }

            if (filter.Month.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(t => t.CreatedDate.Month == filter.Month.Value);
            }

            if (filter.StartDate.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(t => t.CreatedDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(t => t.CreatedDate <= filter.EndDate.Value);
            }

            if (filter.SubscriptionPlanId.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(t => t.UserSubscription.SubscriptionPlanId == filter.SubscriptionPlanId.Value);
            }

            var transactions = await transactionsQuery.ToListAsync();

            var completedTransactions = transactions.Where(t => t.Status == TransactionStatus.COMPLETED).ToList();
            var pendingTransactions = transactions.Where(t => t.Status == TransactionStatus.PENDING).ToList();
            var failedTransactions = transactions.Where(t => t.Status == TransactionStatus.FAILED).ToList();
            var cancelledTransactions = transactions.Where(t => t.Status == TransactionStatus.CANCELLED).ToList();

            var totalRevenue = completedTransactions.Sum(t => t.Price);

            var activeSubscriptionsQuery = _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(us => us.SubscriptionPlan)
                .Where(us => !us.IsDeleted && us.IsActive && us.DateExp > DateTime.UtcNow && us.SubscriptionPlan.Price > 0);

            if (filter.SubscriptionPlanId.HasValue)
            {
                activeSubscriptionsQuery = activeSubscriptionsQuery.Where(us => us.SubscriptionPlanId == filter.SubscriptionPlanId.Value);
            }

            var totalActiveSubscriptions = await activeSubscriptionsQuery.CountAsync();

            var monthlyRevenue = completedTransactions
                .GroupBy(t => new { t.CreatedDate.Year, t.CreatedDate.Month })
                .Select(g => new MonthlyRevenueModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                    Revenue = g.Sum(t => t.Price),
                    TransactionCount = g.Count(),
                    CompletedCount = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            // Load all subscription plans with their subscriptions first
            var allSubscriptionPlansQuery = _unitOfWork.SubscriptionPlanRepository.GetQueryable()
                .Include(sp => sp.UserSubscriptions.Where(us => !us.IsDeleted))
                .Where(sp => !sp.IsDeleted && sp.Price > 0);

            var allSubscriptionPlans = await allSubscriptionPlansQuery.ToListAsync();

            // Now calculate revenue by plan in memory
            var revenueByPlan = allSubscriptionPlans
                .Select(sp => new SubscriptionPlanRevenueModel
                {
                    SubscriptionPlanId = sp.Id,
                    PlanName = sp.Name,
                    PlanPrice = sp.Price,
                    TotalRevenue = completedTransactions
                        .Where(t => t.UserSubscription.SubscriptionPlanId == sp.Id)
                        .Sum(t => t.Price),
                    TotalSubscriptions = sp.UserSubscriptions.Count,
                    ActiveSubscriptions = sp.UserSubscriptions.Count(us => us.IsActive && us.DateExp > DateTime.UtcNow)
                })
                .Where(r => r.TotalRevenue > 0 || r.TotalSubscriptions > 0)
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();

            var recentTransactions = transactions
                .OrderByDescending(t => t.CreatedDate)
                .Take(filter.RecentTransactionLimit)
                .Select(t => new RecentTransactionModel
                {
                    TransactionId = t.Id,
                    UserSubscriptionId = t.UserSubscriptionId,
                    UserId = t.UserSubscription.UserId,
                    UserEmail = t.UserSubscription.User.Email,
                    UserDisplayName = t.UserSubscription.User.DisplayName ?? string.Empty,
                    SubscriptionPlanName = t.UserSubscription.SubscriptionPlan.Name,
                    TransactionCode = t.TransactionCode,
                    Price = t.Price,
                    Status = t.Status.ToString(),
                    CreatedDate = t.CreatedDate
                })
                .ToList();

            var statistics = new RevenueStatisticsModel
            {
                TotalRevenue = totalRevenue,
                TotalTransactions = transactions.Count,
                TotalCompletedTransactions = completedTransactions.Count,
                TotalPendingTransactions = pendingTransactions.Count,
                TotalFailedTransactions = failedTransactions.Count,
                TotalCancelledTransactions = cancelledTransactions.Count,
                TotalActiveSubscriptions = totalActiveSubscriptions,
                MonthlyRevenue = monthlyRevenue,
                RevenueByPlan = revenueByPlan,
                RecentTransactions = recentTransactions
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_REVENUE_STATISTICS_SUCCESS,
                Data = statistics
            };
        }
    }
}
