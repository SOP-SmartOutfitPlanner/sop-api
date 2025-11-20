using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.API.Attributes;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.UserSubscriptionModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/subscriptions")]
    [ApiController]
    [Authorize]
    public class UserSubscriptionController : BaseController
    {
        private readonly IUserSubscriptionService _userSubscriptionService;

        public UserSubscriptionController(IUserSubscriptionService userSubscriptionService)
        {
            _userSubscriptionService = userSubscriptionService;
        }

        /// <summary>
        /// Get all available subscription plans
        /// </summary>
        [HttpGet("plans")]
        [AllowAnonymous]
        public Task<IActionResult> GetAvailablePlans()
        {
            return ValidateAndExecute(async () => await _userSubscriptionService.GetAvailablePlansAsync());
        }

        /// <summary>
        /// Get my current active subscription
        /// </summary>
        [HttpGet("me")]
        public Task<IActionResult> GetMySubscription()
        {
            var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            return ValidateAndExecute(async () => await _userSubscriptionService.GetMySubscriptionAsync(userId));
        }

        /// <summary>
        /// Get my subscription history
        /// </summary>
        [HttpGet("history")]
        public Task<IActionResult> GetSubscriptionHistory()
        {
            var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            return ValidateAndExecute(async () => await _userSubscriptionService.GetSubscriptionHistoryAsync(userId));
        }

        [HttpPost("purchase")]
        public Task<IActionResult> PurchaseSubscription([FromBody] PurchaseSubscriptionRequestModel model)
        {
            var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            return ValidateAndExecute(async () => await _userSubscriptionService.PurchaseSubscriptionAsync(userId, model));
        }

        [HttpDelete("cancel/{transactionId}")]
        public Task<IActionResult> CancelPendingPayment(long transactionId)
        {
            var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            return ValidateAndExecute(async () => await _userSubscriptionService.CancelPendingPaymentAsync(userId, transactionId));
        }
    }
}
