using GenerativeAI.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using PayOS.Resources.V2.PaymentRequests;
using PayOS.Resources.Webhooks;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SOPServer.Service.Services.Implements
{
    public class PayOSService : IPayOSService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOSClient _payos;
        private readonly PayOSSettings _payOSSettings;
        public PayOSService(IOptions<PayOSSettings> payOSSettings, IUnitOfWork unitOfWork)
        {
            _payOSSettings = payOSSettings.Value;
            _unitOfWork = unitOfWork;
            _payos = new PayOSClient(_payOSSettings.ClientId, _payOSSettings.ApiKey, _payOSSettings.ChecksumKey);
        }

        public async Task<CreatePaymentLinkResponse> CreatePaymentUrl(int userSubscriptionId)
        {
            var userSubscription = await _unitOfWork.UserSubscriptionRepository.GetByIdIncludeAsync(
            userSubscriptionId,
            include: q => q.Include(us => us.SubscriptionPlan)
                            .Include(us => us.UserSubscriptionTransactions)
                            .Include(us => us.User)
            );


            List<PaymentLinkItem> items = new List<PaymentLinkItem>();
            items.Add(new PaymentLinkItem
            {
                Name = userSubscription.SubscriptionPlan.Name,
                Quantity = 1,
                Price = (long)userSubscription.SubscriptionPlan.Price
            });

            long expiredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 600; // 10 mins

            var transaction = userSubscription.UserSubscriptionTransactions
                .OrderByDescending(ust => ust.CreatedDate)
                .FirstOrDefault();

            var paymentData =
                new CreatePaymentLinkRequest()
                {
                    OrderCode = transaction.Id,
                    Amount = (long)transaction.Price,
                    Description = MessageConstants.SUBSCRIPTION_TRANSACTION_DESCRIPTION + userSubscription.SubscriptionPlan.Name,
                    Items = items,
                    CancelUrl = _payOSSettings.CancelUrl ?? "https://smartoutfitplanner.vercel.app/cancel",
                    ReturnUrl = _payOSSettings.ReturnUrl ?? "https://smartoutfitplanner.vercel.app/payment-success",
                    ExpiredAt = expiredAt,
                    BuyerName = userSubscription.User.DisplayName,
                    BuyerEmail = userSubscription.User.Email,
                    BuyerAddress = userSubscription.User.Location,

                };
            return await _payos.PaymentRequests.CreateAsync(paymentData);
        }

        public async Task<ActionResult<PaymentLink>> CancelPayment(int transactionId, string cancellationReason)
        {
            var cancelPayment = await _payos.PaymentRequests.CancelAsync(transactionId, cancellationReason ?? "Cancelled by user");
            return cancelPayment;
        }

        public async Task<ActionResult<PaymentLink>> GetPaymentLinkDetails(int transactionId)
        {
            var paymentLinkDetails = await _payos.PaymentRequests.GetAsync(transactionId);
            return paymentLinkDetails;
        }
        public async Task<ConfirmWebhookResponse> ConfirmWebhook(string webhookUrl)
        {
            var confirmWebhook = await _payos.Webhooks.ConfirmAsync(webhookUrl);
            return confirmWebhook;
        }
        public async Task<WebhookData> VerifyPaymentWebhookAsync(Webhook webhook)
        {
            var webhookData = await _payos.Webhooks.VerifyAsync(webhook);
            return webhookData;
        }
    }
}
