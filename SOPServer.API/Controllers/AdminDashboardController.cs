using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.AdminDashboardModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/admin-dashboard")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class AdminDashboardController : BaseController
    {
        private readonly IAdminDashboardService _adminDashboardService;

        public AdminDashboardController(IAdminDashboardService adminDashboardService)
        {
            _adminDashboardService = adminDashboardService;
        }

        /// <summary>
        /// [ADMIN] Get dashboard overview statistics
        /// </summary>
        /// <returns>Dashboard overview with key metrics and percentage changes</returns>
        /// <remarks>
        /// Returns comprehensive dashboard statistics including:
        /// - **Total Users**: Current user count with percentage change from last month
        /// - **Total Items**: Current item count with percentage change from last month
        /// - **Revenue Today**: Today's completed transaction revenue with percentage change from yesterday
        /// - **Community Posts Today**: Today's post count with percentage change from yesterday
        /// 
        /// Each statistic includes:
        /// - `value`: Current value
        /// - `percentageChange`: Percentage change from comparison period
        /// - `changeDirection`: "up", "down", or "neutral"
        /// 
        /// **Comparison Periods:**
        /// - Total Users & Total Items: Compare with last month (30 days ago)
        /// - Revenue Today & Community Posts Today: Compare with yesterday
        /// </remarks>
        /// <response code="200">Dashboard overview retrieved successfully</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        [HttpGet("overview")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetDashboardOverview()
        {
            return ValidateAndExecute(async () => 
                await _adminDashboardService.GetDashboardOverviewAsync());
        }

        /// <summary>
        /// [ADMIN] Get user growth statistics by month
        /// </summary>
        /// <param name="year">Target year (optional, defaults to current year)</param>
        /// <returns>Monthly user growth data with new and active users</returns>
        /// <remarks>
        /// Returns user growth statistics for each month of the specified year including:
        /// - **Month**: Month number (1-12)
        /// - **Year**: Target year
        /// - **MonthName**: Full month name
        /// - **NewUsers**: Number of users registered in that month
        /// - **ActiveUsers**: Cumulative count of active users up to that month
        /// 
        /// **Query Parameters:**
        /// - `year` (optional): Target year to get statistics (defaults to current year)
        /// 
        /// Returns data for all 12 months even if no users registered in some months.
        /// </remarks>
        /// <response code="200">User growth statistics retrieved successfully</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        [HttpGet("user-growth")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetUserGrowthByMonth([FromQuery] int? year = null)
        {
            return ValidateAndExecute(async () => 
                await _adminDashboardService.GetUserGrowthByMonthAsync(year));
        }

        /// <summary>
        /// [ADMIN] Get items distribution by parent category
        /// </summary>
        /// <returns>Item count and percentage for each parent category</returns>
        /// <remarks>
        /// Returns distribution of wardrobe items grouped by parent categories including:
        /// - **CategoryId**: Parent category ID
        /// - **CategoryName**: Parent category name
        /// - **ItemCount**: Number of items in this parent category
        /// - **Percentage**: Percentage of total items
        /// 
        /// **Notes:**
        /// - Items are grouped by their parent category (root level)
        /// - If an item's category has no parent, it uses its own category
        /// - Only non-deleted items are counted
        /// - Results are ordered by item count (descending)
        /// </remarks>
        /// <response code="200">Items by category statistics retrieved successfully</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        [HttpGet("items-by-category")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetItemsByCategory()
        {
            return ValidateAndExecute(async () => 
                await _adminDashboardService.GetItemsByCategoryAsync());
        }

        /// <summary>
        /// [ADMIN] Get weekly activity statistics
        /// </summary>
        /// <returns>Daily breakdown of new users and items for current week</returns>
        /// <remarks>
        /// Returns weekly activity statistics for the current week (Monday to Sunday) including:
        /// - **DayOfWeek**: Day name (Mon, Tue, Wed, Thu, Fri, Sat, Sun)
        /// - **Date**: Full date for that day
        /// - **NewUsers**: Number of users registered on that day
        /// - **NewItems**: Number of items created on that day
        /// 
        /// **Notes:**
        /// - Week starts on Monday and ends on Sunday
        /// - Returns data for all 7 days of current week
        /// - Only counts non-deleted users and items
        /// - Days with no activity will show 0 for both users and items
        /// </remarks>
        /// <response code="200">Weekly activity statistics retrieved successfully</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        [HttpGet("weekly-activity")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetWeeklyActivity()
        {
            return ValidateAndExecute(async () => 
                await _adminDashboardService.GetWeeklyActivityAsync());
        }

        /// <summary>
        /// [ADMIN] Get revenue statistics from user subscriptions
        /// </summary>
        /// <param name="filter">Revenue filter parameters</param>
        /// <returns>Revenue statistics including total revenue, transactions, monthly breakdown, and revenue by plan</returns>
        /// <remarks>
        /// Returns comprehensive revenue statistics from completed subscription transactions including:
        /// - Total revenue from completed transactions
        /// - Transaction counts by status (completed, pending, failed, cancelled)
        /// - Total active subscriptions
        /// - Monthly revenue breakdown with transaction counts
        /// - Revenue grouped by subscription plan
        /// - Recent transactions with user details
        /// 
        /// **Query Parameters:**
        /// - `year` (optional): Filter total statistics by specific year (does NOT affect monthly breakdown)
        /// - `month` (optional): Filter total statistics by specific month 1-12 (does NOT affect monthly breakdown)
        /// - `startDate` (optional): Filter from this date (applies to all sections including monthly breakdown)
        /// - `endDate` (optional): Filter to this date (applies to all sections including monthly breakdown)
        /// - `subscriptionPlanId` (optional): Filter by subscription plan (applies to all sections)
        /// - `recentTransactionLimit` (optional): Number of recent transactions to return (default: 10, max: 100)
        /// 
        /// **Important Notes:**
        /// - Monthly revenue breakdown always shows all months within the date range, ignoring year/month filters
        /// - Year/month filters only affect total statistics (TotalRevenue, TotalTransactions, etc.)
        /// - Only counts transactions with subscription plans that have price > 0 (excludes free plans)
        /// </remarks>
        /// <response code="200">Revenue statistics retrieved successfully</response>
        /// <response code="400">Invalid filter parameters</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        [HttpGet("revenue")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetRevenueStatistics([FromQuery] RevenueFilterModel filter)
        {
            return ValidateAndExecute(async () => 
                await _adminDashboardService.GetRevenueStatisticsAsync(filter));
        }
    }
}
