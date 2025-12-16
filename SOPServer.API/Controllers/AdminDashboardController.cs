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
