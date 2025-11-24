using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ReportCommunityModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    /// <summary>
    /// Controller for managing community content reports
    /// </summary>
    [Route("api/v1/reports")]
    [ApiController]
    public class ReportCommunityController : BaseController
    {
        private readonly IReportCommunityService _reportCommunityService;

        public ReportCommunityController(IReportCommunityService reportCommunityService)
        {
            _reportCommunityService = reportCommunityService;
        }

        /// <summary>
        /// Create a new report for inappropriate community content
        /// </summary>
        /// <param name="model">Report details including type (POST/COMMENT), target ID, and description</param>
        /// <returns>Created report with status PENDING</returns>
        /// <response code="201">Report created successfully</response>
        /// <response code="400">Invalid request or duplicate report</response>
        /// <response code="404">User, post, or comment not found</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/v1/reports
        ///     {
        ///        "userId": 1,
        ///        "postId": 123,
        ///        "commentId": null,
        ///        "type": "POST",
        ///        "description": "This post contains inappropriate content"
        ///     }
        ///     
        /// Note: 
        /// - When reporting a POST, provide postId and set type to "POST"
        /// - When reporting a COMMENT, provide commentId and set type to "COMMENT"
        /// - Each user can only report the same content once
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ReportCommunityModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> CreateReport([FromBody] ReportCommunityCreateModel model)
        {
            return ValidateAndExecute(async () => await _reportCommunityService.CreateReportAsync(model));
        }

        /// <summary>
        /// [ADMIN] Get paginated list of pending reports with optional filters
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters (PageIndex, PageSize)</param>
        /// <param name="filter">Optional filters (Type, FromDate, ToDate)</param>
        /// <returns>Paginated list of pending reports with reporter, content, and author details</returns>
        /// <response code="200">Reports retrieved successfully</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/v1/reports/pending?PageIndex=1&amp;PageSize=20&amp;Type=POST&amp;FromDate=2024-01-01
        ///     
        /// Note: 
        /// - Requires ADMIN role
        /// - Only returns reports with Status=PENDING
        /// - Filter by Type (POST/COMMENT), date range optional
        /// </remarks>
        [HttpGet("pending")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetPendingReports(
            [FromQuery] PaginationParameter paginationParameter,
            [FromQuery] ReportFilterModel filter)
        {
            return ValidateAndExecute(async () =>
                await _reportCommunityService.GetPendingReportsAsync(filter, paginationParameter));
        }

        /// <summary>
        /// [ADMIN] Get paginated list of all reports with optional filters
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters (PageIndex, PageSize)</param>
        /// <param name="filter">Optional filters (Type, Status, FromDate, ToDate)</param>
        /// <returns>Paginated list of all reports with reporter, content, and author details</returns>
        /// <response code="200">Reports retrieved successfully</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/v1/reports?PageIndex=1&amp;PageSize=20&amp;Type=POST&amp;Status=RESOLVED&amp;FromDate=2024-01-01&amp;ToDate=2024-12-31
        ///     
        /// Note: 
        /// - Requires ADMIN role
        /// - Returns all reports regardless of status
        /// - Filter by Type (POST/COMMENT), Status (PENDING/RESOLVED/REJECTED), and date range
        /// - All filters are optional
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetAllReports(
            [FromQuery] PaginationParameter paginationParameter,
            [FromQuery] ReportFilterModel filter)
        {
            return ValidateAndExecute(async () =>
                await _reportCommunityService.GetAllReportsAsync(filter, paginationParameter));
        }

        /// <summary>
        /// [ADMIN] Get detailed information about a specific report
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <returns>Full report details including author violation history and reporter count</returns>
        /// <response code="200">Report details retrieved successfully</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        /// <response code="404">Report not found</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/v1/reports/123/details
        ///     
        /// Response includes:
        /// - Original reporter information (first reporter)
        /// - Total reporter count
        /// - Reported content (post or comment)
        /// - Author information
        /// - Author's warning count (last 90 days)
        /// - Author's suspension count (all time)
        /// </remarks>
        [HttpGet("{id}/details")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetReportDetails(long id)
        {
            return ValidateAndExecute(async () =>
                await _reportCommunityService.GetReportDetailsAsync(id));
        }

        /// <summary>
        /// [ADMIN] Get paginated list of all reporters for a specific report
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <param name="paginationParameter">Pagination parameters (PageIndex, PageSize)</param>
        /// <returns>Paginated list of all users who reported this content</returns>
        /// <response code="200">Reporters retrieved successfully</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        /// <response code="404">Report not found</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/v1/reports/123/reporters?PageIndex=1&amp;PageSize=20
        ///     
        /// Response includes:
        /// - List of users who reported the content
        /// - Each reporter's description of the issue
        /// - Date when each user reported
        /// </remarks>
        [HttpGet("{id}/reporters")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetReporters(long id, [FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () =>
                await _reportCommunityService.GetReportersByReportIdAsync(id, paginationParameter));
        }

        /// <summary>
        /// [ADMIN] Resolve report as no violation found
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <param name="model">Optional resolution notes</param>
        /// <returns>Updated report with status RESOLVED and action NONE</returns>
        /// <response code="200">Report resolved successfully</response>
        /// <response code="400">Report already resolved</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        /// <response code="404">Report not found</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/v1/reports/123/resolve-no-violation
        ///     {
        ///        "notes": "Content reviewed - no policy violation found"
        ///     }
        ///     
        /// Actions taken:
        /// - Report status changed to RESOLVED with action NONE
        /// - Reporter receives notification
        /// - No action taken against content or author
        /// </remarks>
        [HttpPost("{id}/resolve-no-violation")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> ResolveNoViolation(long id, [FromBody] ResolveNoViolationModel model)
        {
            // Extract admin ID from JWT claims
            var adminIdClaim = User.FindFirst("UserId")?.Value;
            if (!long.TryParse(adminIdClaim, out long adminId))
            {
                return Task.FromResult<IActionResult>(Unauthorized("Invalid admin credentials"));
            }

            return ValidateAndExecute(async () =>
                await _reportCommunityService.ResolveNoViolationAsync(id, adminId, model));
        }

        /// <summary>
        /// [ADMIN] Resolve report with enforcement action
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <param name="model">Action details (Action, Notes, SuspensionDays if SUSPEND)</param>
        /// <returns>Updated report with action applied</returns>
        /// <response code="200">Report resolved and action applied successfully</response>
        /// <response code="400">Invalid action or report already resolved</response>
        /// <response code="401">Unauthorized - Admin role required</response>
        /// <response code="404">Report or content not found</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/v1/reports/123/resolve-with-action
        ///     {
        ///        "action": "SUSPEND",
        ///        "notes": "Repeated violations of community guidelines",
        ///        "suspensionDays": 7
        ///     }
        ///     
        /// Available Actions:
        /// - HIDE: Content hidden but not deleted (IsHidden=true)
        /// - DELETE: Content soft deleted (IsDeleted=true)
        /// - WARN: Warning recorded in user history
        /// - SUSPEND: User suspended for N days (1-365), creates violation record
        /// 
        /// Actions taken:
        /// - Report status changed to RESOLVED with specified action
        /// - Action applied to content/user
        /// - Both reporter and author receive notifications
        /// - For SUSPEND: Creates UserSuspension and UserViolation records
        /// </remarks>
        [HttpPost("{id}/resolve-with-action")]
        [Authorize(Roles = "ADMIN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> ResolveWithAction(long id, [FromBody] ResolveWithActionModel model)
        {
            // Extract admin ID from JWT claims
            var adminIdClaim = User.FindFirst("UserId")?.Value;
            if (!long.TryParse(adminIdClaim, out long adminId))
            {
                return Task.FromResult<IActionResult>(Unauthorized("Invalid admin credentials"));
            }

            return ValidateAndExecute(async () =>
                await _reportCommunityService.ResolveWithActionAsync(id, adminId, model));
        }
    }
}
