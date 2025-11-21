using Microsoft.AspNetCore.SignalR;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Hubs
{
    public class PaymentHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentHub(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task AddToGroup(long userId, long userSubTransId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var userSubTrans = await _unitOfWork.UserSubscriptionTransactionRepository.GetByIdAsync(userSubTransId);
            if (userSubTrans == null)
            {
                throw new NotFoundException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_NOT_FOUND);
            }

            switch(userSubTrans.Status)
            {
                case TransactionStatus.FAILED:
                case TransactionStatus.CANCELLED:
                    throw new NotFoundException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_NOT_FOUND);
                case TransactionStatus.COMPLETED:
                    throw new NotFoundException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_ALREADY_COMPLETED);
                default:
                    break;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, userSubTrans.TransactionCode.ToString());
            Console.WriteLine("ADD USER: GROUP " + userSubTrans.TransactionCode.ToString());
        }

        public async Task RemoveGroup(long userSubTransId)
        {
            var userSubTrans = await _unitOfWork.UserSubscriptionTransactionRepository.GetByIdAsync(userSubTransId);
            if (userSubTrans == null)
            {
                throw new NotFoundException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_NOT_FOUND);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userSubTrans.TransactionCode.ToString());
        }
    }
}
