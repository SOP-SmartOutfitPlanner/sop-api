using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.DashboardModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/stylist/dashboard")]
    [ApiController]
    [Authorize]
    public class StylistDashboardController : BaseController
    {
        private readonly IStylistDashboardService _stylistDashboardService;

        public StylistDashboardController(IStylistDashboardService stylistDashboardService)
        {
            _stylistDashboardService = stylistDashboardService;
        }

        /// <summary>
        /// Get collection statistics for stylist dashboard
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="filter">Dashboard filter parameters</param>
        /// <returns>Collection statistics including total counts, monthly breakdown, and top collections</returns>
        /// <remarks>
        /// **Auth Required** - Users can only access their own dashboard
        ///
        /// Returns comprehensive collection statistics for stylists:
        /// - **Overall Metrics**: Total collections, published/unpublished counts, total engagement
        /// - **Monthly Breakdown**: Collections created and engagement received per month
        /// - **Top Collections**: Ranked by total engagement (likes + comments + saves)
        ///
        /// **Query Parameters:**
        /// - `year`: Filter by specific year (default: current year, range: 2020-2100)
        /// - `month`: Filter by specific month (1-12, optional - if not provided, returns all 12 months)
        /// - `topCollectionsCount`: Number of top collections to return (default: 5, range: 1-50)
        ///
        /// **Usage Examples:**
        /// ```
        /// // Get full year stats for 2024
        /// GET /api/v1/stylist/dashboard/collections/123?year=2024
        ///
        /// // Get specific month (December 2024)
        /// GET /api/v1/stylist/dashboard/collections/123?year=2024&amp;month=12
        ///
        /// // Get top 10 collections
        /// GET /api/v1/stylist/dashboard/collections/123?topCollectionsCount=10
        ///
        /// // Current year with default settings
        /// GET /api/v1/stylist/dashboard/collections/123
        /// ```
        ///
        /// **Response Structure:**
        /// ```json
        /// {
        ///   "statusCode": 200,
        ///   "message": "Collection statistics retrieved successfully",
        ///   "data": {
        ///     "totalCollections": 15,
        ///     "publishedCollections": 12,
        ///     "unpublishedCollections": 3,
        ///     "totalLikes": 284,
        ///     "totalComments": 156,
        ///     "totalSaves": 98,
        ///     "monthlyStats": [...],
        ///     "topCollections": [...]
        ///   }
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="400">Invalid filter parameters</response>
        /// <response code="403">User not authorized to access this dashboard</response>
        /// <response code="404">User not found</response>
        [HttpGet("collections")]
        public Task<IActionResult> GetCollectionStatistics([FromQuery] DashboardFilterModel filter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            var userId = long.Parse(userIdClaim);
            return ValidateAndExecute(async () => 
                await _stylistDashboardService.GetCollectionStatisticsByUserAsync(userId, filter));
        }
    }
}
