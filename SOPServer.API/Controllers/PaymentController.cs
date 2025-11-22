using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PayOS.Models.Webhooks;
using SOPServer.Service.Hubs;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/payments")]
    [ApiController]
    public class PaymentController : BaseController
    {
        private readonly IPayOSService _payOSService;
        private readonly IUserSubscriptionService _userSubscriptionService;
        private readonly IHubContext<PaymentHub> _hubContext;

        public PaymentController(IPayOSService payOSService, IUserSubscriptionService userSubscriptionService, IHubContext<PaymentHub> hubContext)
        {
            _payOSService = payOSService;
            _userSubscriptionService = userSubscriptionService;
            _hubContext = hubContext;
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
                var webhookData = await _payOSService.VerifyPaymentWebhookAsync(webhook);

                var transactionCode = webhookData.OrderCode;
                var paymentStatus = webhookData.Code;

                var result = await _userSubscriptionService.ProcessPaymentWebhookAsync(transactionCode, paymentStatus);
                Console.WriteLine("transactionCode: " + webhookData.OrderCode);
                await _hubContext.Clients.Group(webhookData.OrderCode.ToString()).SendAsync("PaymentStatusUpdated", result);

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                }); 
            }
            catch (Exception ex)    
            {
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
