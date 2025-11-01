using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.UserOccasionModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    /// <summary>
    /// User Occasion management endpoints for creating, viewing, updating, and managing personal calendar events
    /// </summary>
    [Route("api/v1/user-occasions")]
    [ApiController]
    [Authorize(Roles = "USER,STYLIST,ADMIN")]
    public class UserOccasionController : BaseController
    {
        private readonly IUserOccasionService _userOccasionService;

        public UserOccasionController(IUserOccasionService userOccasionService)
        {
            _userOccasionService = userOccasionService;
        }

        /// <summary>
        /// Get all user occasions with pagination and search
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Query Parameters:**
        /// - `page-index`: Page number (default: 1)
        /// - `page-size`: Items per page (default: 10)
        /// - `q`: Search in name or description (optional)
        ///
        /// **Note:** Users can only see their own occasions
        /// </remarks>
        [HttpGet]
        public Task<IActionResult> GetUserOccasions([FromQuery] PaginationParameter paginationParameter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _userOccasionService.GetUserOccasionPaginationAsync(paginationParameter, userId));
        }

        /// <summary>
        /// Get user occasion by ID with full details
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Users can only access their own occasions
        /// </remarks>
        [HttpGet("{id}")]
        public Task<IActionResult> GetUserOccasionById(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _userOccasionService.GetUserOccasionByIdAsync(id, userId));
        }

        /// <summary>
        /// Create a new user occasion/event
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Request Body:**
        /// - `occasionId`: Reference to master Occasion (optional)
        /// - `name`: Name/title of the occasion (required)
        /// - `description`: Detailed description (optional)
        /// - `dateOccasion`: Date of the occasion (required)
        /// - `startTime`: Start time (optional)
        /// - `endTime`: End time (optional)
        /// - `weatherSnapshot`: Weather information (optional)
        ///
        /// **Note:** UserId is extracted from JWT token automatically
        /// </remarks>
        [HttpPost]
        public Task<IActionResult> CreateUserOccasion([FromBody] UserOccasionCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _userOccasionService.CreateUserOccasionAsync(userId, model));
        }

        /// <summary>
        /// Update user occasion details
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Request Body:** All fields are optional, only provided fields will be updated
        /// - `occasionId`: Reference to master Occasion
        /// - `name`: Name/title of the occasion
        /// - `description`: Detailed description
        /// - `dateOccasion`: Date of the occasion
        /// - `startTime`: Start time
        /// - `endTime`: End time
        /// - `weatherSnapshot`: Weather information
        ///
        /// **Note:** Users can only update their own occasions
        /// </remarks>
        [HttpPut("{id}")]
        public Task<IActionResult> UpdateUserOccasion(long id, [FromBody] UserOccasionUpdateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _userOccasionService.UpdateUserOccasionAsync(id, userId, model));
        }

        /// <summary>
        /// Delete user occasion (soft delete)
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Users can only delete their own occasions
        /// </remarks>
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteUserOccasion(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _userOccasionService.DeleteUserOccasionAsync(id, userId));
        }
    }
}
