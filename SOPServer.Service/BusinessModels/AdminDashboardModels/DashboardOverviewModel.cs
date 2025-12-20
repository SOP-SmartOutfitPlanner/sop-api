namespace SOPServer.Service.BusinessModels.AdminDashboardModels
{
    public class DashboardOverviewModel
    {
        public StatisticCardModel TotalUsers { get; set; } = new StatisticCardModel();
        public StatisticCardModel TotalItems { get; set; } = new StatisticCardModel();
        public StatisticCardModel RevenueToday { get; set; } = new StatisticCardModel();
        public StatisticCardModel CommunityPostsToday { get; set; } = new StatisticCardModel();
    }

    public class StatisticCardModel
    {
        public decimal Value { get; set; }
        public decimal PercentageChange { get; set; }
        public string ChangeDirection { get; set; } = string.Empty; // "up", "down", "neutral"
    }

    public class UserGrowthModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class ItemsByCategoryModel
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class WeeklyActivityModel
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int NewUsers { get; set; }
        public int NewItems { get; set; }
    }
}
