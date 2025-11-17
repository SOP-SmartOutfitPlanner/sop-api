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

                // Extract transaction ID from orderCode
                var transactionId = webhookData.OrderCode;

                // TODO: Process payment based on webhook data
                // This will be implemented in the next step
                // - Update transaction status
                // - Activate subscription if payment successful
                // - Handle failed payments

                return Ok(new
                {
                    success = true,
                    message = "Webhook received successfully",
                    orderCode = transactionId
                });
            }
            catch (Exception ex)
            {
                // Log error but return 200 to PayOS to avoid retries
                return Ok(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
