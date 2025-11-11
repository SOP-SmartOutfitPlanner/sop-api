using Microsoft.AspNetCore.Mvc;
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
    }
}
