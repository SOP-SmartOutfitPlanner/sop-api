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
        /// Get collection statistics for authenticated stylist
        /// </summary>
        /// <param name="filter">Dashboard filter parameters (year, month, topCollectionsCount)</param>
        /// <returns>Collection statistics with total counts, monthly breakdown, and top performing collections</returns>
        /// <remarks>
        /// Returns collection statistics for the authenticated user including:
        /// - Total collections (published/unpublished)
        /// - Total engagement (likes, comments, saves)
        /// - Monthly breakdown of collections and engagement
        /// - Top performing collections by engagement
        /// 
        /// **Query Parameters:**
        /// - `year` (optional): Target year, default is current year
        /// - `month` (optional): Target month (1-12), omit for all 12 months
        /// - `topCollectionsCount` (optional): Number of top collections, default is 5
        /// </remarks>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="400">Invalid filter parameters</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("collections")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetCollectionStatistics([FromQuery] DashboardFilterModel filter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            var userId = long.Parse(userIdClaim);
            return ValidateAndExecute(async () => 
                await _stylistDashboardService.GetCollectionStatisticsByUserAsync(userId, filter));
        }

        /// <summary>
        /// Get post statistics for authenticated user
        /// </summary>
        /// <param name="filter">Dashboard filter parameters (year, month, topPostsCount)</param>
        /// <returns>Post statistics with total counts, monthly breakdown, follower counts, and top performing posts</returns>
        /// <remarks>
        /// Returns post statistics for the authenticated user including:
        /// - Total posts
        /// - Total engagement (likes, comments)
        /// - Total followers and followers this month
        /// - Monthly breakdown of posts and engagement
        /// - Top performing posts by engagement
        /// 
        /// **Query Parameters:**
        /// - `year` (optional): Target year, default is current year
        /// - `month` (optional): Target month (1-12), omit for all 12 months
        /// - `topPostsCount` (optional): Number of top posts, default is 5
        /// </remarks>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="400">Invalid filter parameters</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("posts")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetPostStatistics([FromQuery] PostDashboardFilterModel filter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            var userId = long.Parse(userIdClaim);
            return ValidateAndExecute(async () => 
                await _stylistDashboardService.GetPostStatisticsByUserAsync(userId, filter));
        }
    }
}
