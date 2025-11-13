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

        // ========== CREATE ==========
        
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
    }
}
