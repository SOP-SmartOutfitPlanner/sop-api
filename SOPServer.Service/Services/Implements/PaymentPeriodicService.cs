using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayOS.Models.V2.PaymentRequests;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class PaymentPeriodicService : BackgroundService, IPaymentPeriodicService
    {
        private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(30));
        private readonly IServiceScopeFactory _scopeFactory;
        public PaymentPeriodicService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        public async Task UpdatePaymentInfo()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var payOSService = scope.ServiceProvider.GetRequiredService<IPayOSService>();

                    var listOrderPending = await unitOfWork.UserSubscriptionTransactionRepository.GetAllOrderPending();

                    foreach (var orderPending in listOrderPending)
                    {
                        var orderInfo = await payOSService.GetPaymentLinkDetails(orderPending.TransactionCode);
                        switch (orderInfo.Status)
                        {
                            case PaymentLinkStatus.Cancelled:
                            case PaymentLinkStatus.Expired:
                                orderPending.Status = TransactionStatus.FAILED;
                                unitOfWork.UserSubscriptionTransactionRepository.UpdateAsync(orderPending);
                                await unitOfWork.SaveAsync();
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(stoppingToken))
                {
                    await UpdatePaymentInfo();
                }
            }
            catch (OperationCanceledException)
            {

            }
        }
    }
}
