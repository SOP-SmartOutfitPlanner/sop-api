using Microsoft.AspNetCore.Mvc;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IPayOSService
    {
        Task<CreatePaymentLinkResponse> CreatePaymentUrl(int userSubscriptionId);
        Task<ActionResult<PaymentLink>> CancelPayment(int transactionId, string cancellationReason);
        Task<PaymentLink> GetPaymentLinkDetails(int transactionId);
        Task<ConfirmWebhookResponse> ConfirmWebhook(string webhookUrl);
        Task<WebhookData> VerifyPaymentWebhookAsync(Webhook webhook);
    }
}
