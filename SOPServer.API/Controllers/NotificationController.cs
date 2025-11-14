using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.NotificationModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/notifications")]
    [ApiController]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> CreateNotification([FromBody] NotificationRequestModel model)
        {
            return ValidateAndExecute(async () => await _notificationService.CreateNotification(model));
        }

        [HttpPost("push")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> PushNotification([FromBody] NotificationRequestModel model)
        {
            return ValidateAndExecute(async () => await _notificationService.PushNotification(model));
        }

        [HttpPost("user/{userId}")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> PushNotificationByUserId(long userId, [FromBody] NotificationRequestModel model)
        {
            return ValidateAndExecute(async () => await _notificationService.PushNotificationByUserId(userId, model));
        }

        // ========== READ ==========

        [HttpGet("{id}")]
        [Authorize]
        public Task<IActionResult> GetNotificationById(long id)
        {
            return ValidateAndExecute(async () => await _notificationService.GetNotificationById(id));
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> GetAllNotifications([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _notificationService.GetAllNotifications(paginationParameter));
        }

        [HttpGet("system")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> GetSystemNotifications(
            [FromQuery] PaginationParameter paginationParameter,
            [FromQuery] bool newestFirst = true,
            [FromQuery] string? searchTerm = null)
        {
            return ValidateAndExecute(async () => await _notificationService.GetSystemNotifications(paginationParameter, newestFirst, searchTerm));
        }

        [HttpGet("user/{userId}")]
        [Authorize]
        public Task<IActionResult> GetNotificationsByUserId(
            [FromQuery] PaginationParameter paginationParameter,
            long userId,
            [FromQuery] int type = 0)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || long.Parse(userIdClaim) != userId)
            {
                return Task.FromResult<IActionResult>(Forbid());
            }

            return ValidateAndExecute(async () => await _notificationService.GetNotificationsByUserId(paginationParameter, userId, type));
        }

        [HttpGet("user/{userId}/unread-count")]
        [Authorize]
        public Task<IActionResult> GetUnreadNotificationCount(long userId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || long.Parse(userIdClaim) != userId)
            {
                return Task.FromResult<IActionResult>(Forbid());
            }

            return ValidateAndExecute(async () => await _notificationService.GetUnreadNotificationCount(userId));
        }

        // ========== MARK AS READ ==========

        [HttpPut("{notificationId}/read")]
        [Authorize]
        public Task<IActionResult> MarkNotificationAsRead(long notificationId)
        {
            return ValidateAndExecute(async () => await _notificationService.MarkNotificationAsRead(notificationId));
        }

        [HttpPut("user/{userId}/read-all")]
        [Authorize]
        public Task<IActionResult> MarkAllNotificationsAsRead(long userId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || long.Parse(userIdClaim) != userId)
            {
                return Task.FromResult<IActionResult>(Forbid());
            }

            return ValidateAndExecute(async () => await _notificationService.MarkAllNotificationsAsRead(userId));
        }

        // ========== DELETE ==========

        /// <summary>
        /// Delete multiple notifications by their IDs for the authenticated user
        /// </summary>
        /// <param name="model">List of notification IDs to delete</param>
        /// <returns>Result of the deletion operation</returns>
        /// <response code="200">Notifications deleted successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">User or notifications not found</response>
        /// <remarks>
        /// Sample request:
        ///
        ///     DELETE /api/v1/notifications
        ///     {
        ///        "notificationIds": [1, 2, 3, 4, 5]
        ///     }
        ///     
        /// **Auth Required:** User ID is extracted from JWT token automatically
        /// 
        /// **Features:**
        /// - Soft deletes notifications (can be restored if needed)
        /// - Only deletes notifications that belong to the authenticated user
        /// - Returns count of deleted vs requested notifications
        /// - Validates user exists before processing
        /// - Automatically filters out notifications that don't belong to user
        /// 
        /// **Response includes:**
        /// - DeletedCount: Number of notifications successfully deleted
        /// - RequestedCount: Number of notification IDs provided
        /// - Message indicates if some notifications were not found or don't belong to user
        /// </remarks>
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> DeleteNotificationsByIds([FromBody] DeleteNotificationsModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
            {
                return Task.FromResult<IActionResult>(Unauthorized());
            }

            return ValidateAndExecute(async () => await _notificationService.DeleteNotificationsByIdsAsync(userId, model));
        }
    }
}
