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
            // ========== MAIN TRANSACTIONS QUERY (with all filters for totals) ==========
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

            // ========== MONTHLY REVENUE QUERY (without Year/Month filters) ==========
            var monthlyTransactionsQuery = _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                .Include(t => t.UserSubscription)
                    .ThenInclude(us => us.SubscriptionPlan)
                .Where(t => !t.IsDeleted 
                    && t.Status == TransactionStatus.COMPLETED 
                    && t.UserSubscription.SubscriptionPlan.Price > 0);

            // Only apply StartDate/EndDate and SubscriptionPlanId filters for monthly breakdown
            if (filter.StartDate.HasValue)
            {
                monthlyTransactionsQuery = monthlyTransactionsQuery.Where(t => t.CreatedDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                monthlyTransactionsQuery = monthlyTransactionsQuery.Where(t => t.CreatedDate <= filter.EndDate.Value);
            }

            if (filter.SubscriptionPlanId.HasValue)
            {
                monthlyTransactionsQuery = monthlyTransactionsQuery.Where(t => t.UserSubscription.SubscriptionPlanId == filter.SubscriptionPlanId.Value);
            }

            var monthlyTransactions = await monthlyTransactionsQuery.ToListAsync();

            var monthlyRevenue = monthlyTransactions
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

            // ========== ACTIVE SUBSCRIPTIONS COUNT ==========
            var activeSubscriptionsQuery = _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(us => us.SubscriptionPlan)
                .Where(us => !us.IsDeleted && us.IsActive && us.DateExp > DateTime.UtcNow && us.SubscriptionPlan.Price > 0);

            if (filter.SubscriptionPlanId.HasValue)
            {
                activeSubscriptionsQuery = activeSubscriptionsQuery.Where(us => us.SubscriptionPlanId == filter.SubscriptionPlanId.Value);
            }

            var totalActiveSubscriptions = await activeSubscriptionsQuery.CountAsync();

            // ========== REVENUE BY PLAN ==========
            var revenueByPlanQuery = _unitOfWork.SubscriptionPlanRepository.GetQueryable()
                .Include(sp => sp.UserSubscriptions.Where(us => !us.IsDeleted))
                .Where(sp => !sp.IsDeleted && sp.Price > 0);

            if (filter.SubscriptionPlanId.HasValue)
            {
                revenueByPlanQuery = revenueByPlanQuery.Where(sp => sp.Id == filter.SubscriptionPlanId.Value);
            }

            var subscriptionPlans = await revenueByPlanQuery.ToListAsync();

            var revenueByPlan = subscriptionPlans
                .Select(sp =>
                {
                    var planCompletedTransactions = completedTransactions
                        .Where(t => t.UserSubscription.SubscriptionPlanId == sp.Id)
                        .ToList();

                    return new SubscriptionPlanRevenueModel
                    {
                        SubscriptionPlanId = sp.Id,
                        PlanName = sp.Name,
                        PlanPrice = sp.Price,
                        TotalRevenue = planCompletedTransactions.Sum(t => t.Price),
                        TotalSubscriptions = sp.UserSubscriptions.Count,
                        ActiveSubscriptions = sp.UserSubscriptions.Count(us => us.IsActive && us.DateExp > DateTime.UtcNow)
                    };
                })
                .Where(r => r.TotalRevenue > 0 || r.TotalSubscriptions > 0)
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();

            // ========== RECENT TRANSACTIONS ==========
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

            // ========== BUILD RESPONSE ==========
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

        public async Task<BaseResponseModel> GetDashboardOverviewAsync()
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var lastMonth = today.AddMonths(-1);

            // ========== TOTAL USERS ==========
            var totalUsers = await _unitOfWork.UserRepository.GetQueryable()
                .Where(u => !u.IsDeleted)
                .CountAsync();

            var usersLastMonth = await _unitOfWork.UserRepository.GetQueryable()
                .Where(u => !u.IsDeleted && u.CreatedDate < lastMonth)
                .CountAsync();

            var userPercentageChange = usersLastMonth > 0 
                ? ((decimal)(totalUsers - usersLastMonth) / usersLastMonth) * 100 
                : 0;

            // ========== TOTAL ITEMS ==========
            var totalItems = await _unitOfWork.ItemRepository.GetQueryable()
                .Where(i => !i.IsDeleted)
                .CountAsync();

            var itemsLastMonth = await _unitOfWork.ItemRepository.GetQueryable()
                .Where(i => !i.IsDeleted && i.CreatedDate < lastMonth)
                .CountAsync();

            var itemPercentageChange = itemsLastMonth > 0 
                ? ((decimal)(totalItems - itemsLastMonth) / itemsLastMonth) * 100 
                : 0;

            // ========== REVENUE TODAY ==========
            var revenueTodayAmount = await _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                .Include(t => t.UserSubscription)
                    .ThenInclude(us => us.SubscriptionPlan)
                .Where(t => !t.IsDeleted 
                    && t.Status == TransactionStatus.COMPLETED
                    && t.CreatedDate >= today
                    && t.UserSubscription.SubscriptionPlan.Price > 0)
                .SumAsync(t => t.Price);

            var revenueYesterdayAmount = await _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                .Include(t => t.UserSubscription)
                    .ThenInclude(us => us.SubscriptionPlan)
                .Where(t => !t.IsDeleted 
                    && t.Status == TransactionStatus.COMPLETED
                    && t.CreatedDate >= yesterday 
                    && t.CreatedDate < today
                    && t.UserSubscription.SubscriptionPlan.Price > 0)
                .SumAsync(t => t.Price);

            var revenuePercentageChange = revenueYesterdayAmount > 0 
                ? ((revenueTodayAmount - revenueYesterdayAmount) / revenueYesterdayAmount) * 100 
                : 0;

            // ========== COMMUNITY POSTS TODAY ==========
            var postsToday = await _unitOfWork.PostRepository.GetQueryable()
                .Where(p => !p.IsDeleted && p.CreatedDate >= today)
                .CountAsync();

            var postsYesterday = await _unitOfWork.PostRepository.GetQueryable()
                .Where(p => !p.IsDeleted && p.CreatedDate >= yesterday && p.CreatedDate < today)
                .CountAsync();

            var postPercentageChange = postsYesterday > 0 
                ? ((decimal)(postsToday - postsYesterday) / postsYesterday) * 100 
                : 0;

            // ========== BUILD RESPONSE ==========
            var overview = new DashboardOverviewModel
            {
                TotalUsers = new StatisticCardModel
                {
                    Value = totalUsers,
                    PercentageChange = Math.Round(userPercentageChange, 1),
                    ChangeDirection = userPercentageChange > 0 ? "up" : userPercentageChange < 0 ? "down" : "neutral"
                },
                TotalItems = new StatisticCardModel
                {
                    Value = totalItems,
                    PercentageChange = Math.Round(itemPercentageChange, 1),
                    ChangeDirection = itemPercentageChange > 0 ? "up" : itemPercentageChange < 0 ? "down" : "neutral"
                },
                RevenueToday = new StatisticCardModel
                {
                    Value = revenueTodayAmount,
                    PercentageChange = Math.Round(revenuePercentageChange, 1),
                    ChangeDirection = revenuePercentageChange > 0 ? "up" : revenuePercentageChange < 0 ? "down" : "neutral"
                },
                CommunityPostsToday = new StatisticCardModel
                {
                    Value = postsToday,
                    PercentageChange = Math.Round(postPercentageChange, 1),
                    ChangeDirection = postPercentageChange > 0 ? "up" : postPercentageChange < 0 ? "down" : "neutral"
                }
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_DASHBOARD_OVERVIEW_SUCCESS,
                Data = overview
            };
        }

        public async Task<BaseResponseModel> GetUserGrowthByMonthAsync(int? year = null)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;

            // ========== GET ALL USERS GROUPED BY MONTH ==========
            var allUsers = await _unitOfWork.UserRepository.GetQueryable()
                .Where(u => !u.IsDeleted && u.CreatedDate.Year == targetYear)
                .ToListAsync();

            var usersByMonth = allUsers
                .GroupBy(u => u.CreatedDate.Month)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ========== CALCULATE MONTHLY STATISTICS ==========
            var monthlyStats = new List<UserGrowthModel>();
            var cumulativeUsers = 0;

            for (int month = 1; month <= 12; month++)
            {
                var newUsersThisMonth = usersByMonth.ContainsKey(month) ? usersByMonth[month].Count : 0;
                cumulativeUsers += newUsersThisMonth;

                monthlyStats.Add(new UserGrowthModel
                {
                    Month = month,
                    Year = targetYear,
                    MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                    NewUsers = newUsersThisMonth,
                    ActiveUsers = cumulativeUsers
                });
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_USER_GROWTH_SUCCESS,
                Data = monthlyStats
            };
        }

        public async Task<BaseResponseModel> GetItemsByCategoryAsync()
        {
            // ========== GET ALL ITEMS WITH CATEGORIES ==========
            var items = await _unitOfWork.ItemRepository.GetQueryable()
                .Include(i => i.Category)
                    .ThenInclude(c => c.Parent)
                .Where(i => !i.IsDeleted && i.CategoryId != null)
                .ToListAsync();

            var totalItems = items.Count;

            if (totalItems == 0)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.GET_ITEMS_BY_CATEGORY_SUCCESS,
                    Data = new List<ItemsByCategoryModel>()
                };
            }

            // ========== GROUP BY PARENT CATEGORY ==========
            var itemsByParentCategory = items
                .GroupBy(i => new
                {
                    CategoryId = i.Category.ParentId ?? i.CategoryId.Value,
                    CategoryName = i.Category.ParentId.HasValue 
                        ? i.Category.Parent.Name 
                        : i.Category.Name
                })
                .Select(g => new ItemsByCategoryModel
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    ItemCount = g.Count(),
                    Percentage = Math.Round((decimal)g.Count() / totalItems * 100, 1)
                })
                .OrderByDescending(c => c.ItemCount)
                .ToList();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_ITEMS_BY_CATEGORY_SUCCESS,
                Data = itemsByParentCategory
            };
        }

        public async Task<BaseResponseModel> GetWeeklyActivityAsync()
        {
            // ========== CALCULATE CURRENT WEEK RANGE ==========
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            // Monday = 1, Sunday = 0 -> Adjust to make Monday start of week
            var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            var startOfWeek = today.AddDays(-daysFromMonday);
            var endOfWeek = startOfWeek.AddDays(7);

            // ========== GET USERS AND ITEMS FOR CURRENT WEEK ==========
            var users = await _unitOfWork.UserRepository.GetQueryable()
                .Where(u => !u.IsDeleted && u.CreatedDate >= startOfWeek && u.CreatedDate < endOfWeek)
                .ToListAsync();

            var items = await _unitOfWork.ItemRepository.GetQueryable()
                .Where(i => !i.IsDeleted && i.CreatedDate >= startOfWeek && i.CreatedDate < endOfWeek)
                .ToListAsync();

            // ========== GROUP BY DAY ==========
            var usersByDay = users
                .GroupBy(u => u.CreatedDate.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var itemsByDay = items
                .GroupBy(i => i.CreatedDate.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            // ========== BUILD WEEKLY ACTIVITY ==========
            var weeklyActivity = new List<WeeklyActivityModel>();
            var daysOfWeek = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                
                weeklyActivity.Add(new WeeklyActivityModel
                {
                    DayOfWeek = daysOfWeek[i],
                    Date = date,
                    NewUsers = usersByDay.ContainsKey(date) ? usersByDay[date] : 0,
                    NewItems = itemsByDay.ContainsKey(date) ? itemsByDay[date] : 0
                });
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_WEEKLY_ACTIVITY_SUCCESS,
                Data = weeklyActivity
            };
        }
    }
}
