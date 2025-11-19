using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/payments")]
    [ApiController]
    public class PaymentController : BaseController
    {
        private readonly IPayOSService _payOSService;
        private readonly IUserSubscriptionService _userSubscriptionService;

        public PaymentController(IPayOSService payOSService, IUserSubscriptionService userSubscriptionService)
        {
            _payOSService = payOSService;
            _userSubscriptionService = userSubscriptionService;
        }

        /// <summary>
        /// PayOS webhook endpoint to receive payment notifications
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePaymentWebhook([FromBody] Webhook webhook)
        {
            try
            {
                // Verify webhook signature from PayOS
                var webhookData = await _payOSService.VerifyPaymentWebhookAsync(webhook);

                // Extract transaction ID and payment status
                var transactionId = webhookData.OrderCode;
                var paymentStatus = webhookData.Code; // "00" = success, other codes = failed

                // Process payment and update subscription status
                var result = await _userSubscriptionService.ProcessPaymentWebhookAsync(transactionId, paymentStatus);

                // Return success to PayOS (always 200 OK to prevent retries)
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                // Log error but return 200 to PayOS to avoid retries
                // PayOS will not retry if we return 200
                return Ok(new
                {
                    success = false,
                    message = $"Webhook processing error: {ex.Message}",
                    error = ex.GetType().Name
                });
            }
        }
    }
}
