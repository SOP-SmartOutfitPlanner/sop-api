using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.AdminDashboardModels
{
    public class RevenueStatisticsModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalCompletedTransactions { get; set; }
        public int TotalPendingTransactions { get; set; }
        public int TotalFailedTransactions { get; set; }
        public int TotalCancelledTransactions { get; set; }
        public int TotalActiveSubscriptions { get; set; }
        public List<MonthlyRevenueModel> MonthlyRevenue { get; set; } = new List<MonthlyRevenueModel>();
        public List<SubscriptionPlanRevenueModel> RevenueByPlan { get; set; } = new List<SubscriptionPlanRevenueModel>();
        public List<RecentTransactionModel> RecentTransactions { get; set; } = new List<RecentTransactionModel>();
    }

    public class MonthlyRevenueModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class SubscriptionPlanRevenueModel
    {
        public long SubscriptionPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
    }

    public class RecentTransactionModel
    {
        public long TransactionId { get; set; }
        public long UserSubscriptionId { get; set; }
        public long UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public string SubscriptionPlanName { get; set; } = string.Empty;
        public int TransactionCode { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class RevenueFilterModel
    {
        [Range(2020, 2100, ErrorMessage = "Year must be between 2020 and 2100")]
        public int? Year { get; set; }

        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        public int? Month { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public long? SubscriptionPlanId { get; set; }

        [Range(1, 100, ErrorMessage = "Recent transaction limit must be between 1 and 100")]
        public int RecentTransactionLimit { get; set; } = 10;
    }
}
