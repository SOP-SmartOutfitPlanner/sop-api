using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SubscriptionLimitModels;
using SOPServer.Service.BusinessModels.SubscriptionPlanModels;
using SOPServer.Service.BusinessModels.UserSubscriptionModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Text.Json;

namespace SOPServer.Service.Services.Implements
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPayOSService _payOSService;

        public UserSubscriptionService(IUnitOfWork unitOfWork, IMapper mapper, IPayOSService payOSService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _payOSService = payOSService;
        }

        public async Task<BaseResponseModel> PurchaseSubscriptionAsync(long userId, PurchaseSubscriptionRequestModel model)
        {
            // Get the subscription plan
            var plan = await _unitOfWork.SubscriptionPlanRepository.GetByIdAsync(model.SubscriptionPlanId);
            if (plan == null)
                throw new NotFoundException(MessageConstants.SUBSCRIPTION_PLAN_NOT_FOUND);

            // Check plan status and user eligibility
            if (plan.Status == SubscriptionPlanStatus.DRAFT)
                throw new BadRequestException("This subscription plan is not available for purchase yet.");

            if (plan.Status == SubscriptionPlanStatus.ARCHIVED)
                throw new BadRequestException("This subscription plan is no longer available for purchase.");

            // For INACTIVE plans, only users who previously had this plan can rebuy
            if (plan.Status == SubscriptionPlanStatus.INACTIVE)
            {
                var hasPreviousSubscription = (await _unitOfWork.UserSubscriptionRepository.GetAllAsync())
                    .Any(s => s.UserId == userId && s.SubscriptionPlanId == plan.Id);

                if (!hasPreviousSubscription)
                    throw new BadRequestException("This subscription plan is only available for existing customers.");
            }

            // Check if user already has an active subscription
            var existingSubscription = (await _unitOfWork.UserSubscriptionRepository.GetAllAsync())
                .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow)
                .FirstOrDefault();

            if (existingSubscription != null)
                throw new BadRequestException("You already have an active subscription. Please wait until it expires before purchasing a new one.");

            // Check if user has a pending payment
            var pendingSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.UserSubscriptionTransactions)
                .Where(s => s.UserId == userId && !s.IsActive)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefaultAsync();

            if (pendingSubscription != null)
            {
                var pendingTransaction = pendingSubscription.UserSubscriptionTransactions
                    .FirstOrDefault(t => t.Status == TransactionStatus.PENDING);

                if (pendingTransaction != null)
                {
                    // User already has a pending payment, return existing payment URL
                    var existingPaymentLink = await _payOSService.CreatePaymentUrl((int)pendingSubscription.Id);
                    return new BaseResponseModel
                    {
                        StatusCode = StatusCodes.Status200OK,
                        Message = "You already have a pending payment. Please complete it before creating a new one.",
                        Data = new
                        {
                            PaymentUrl = existingPaymentLink.CheckoutUrl,
                            Amount = plan.Price,
                            SubscriptionPlanName = plan.Name,
                            TransactionId = pendingTransaction.Id,
                            ExpiredAt = existingPaymentLink.ExpiredAt
                        }
                    };
                }
            }

            // Deserialize plan benefits to initialize user benefits
            var planBenefits = DeserializeBenefitLimit(plan.BenefitLimit);

            // Create user subscription with PENDING status (IsActive = false)
            var userSubscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                DateExp = DateTime.UtcNow.AddMonths(1), // 1 month subscription
                IsActive = false, // Will be activated after payment
                BenefitUsed = JsonSerializer.Serialize(planBenefits), // Initialize with full credits
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.UserSubscriptionRepository.AddAsync(userSubscription);
            await _unitOfWork.SaveAsync();

            // Create transaction record with PENDING status
            var transaction = new UserSubscriptionTransaction
            {
                UserSubscriptionId = userSubscription.Id,
                TransactionCode = $"TXN-{DateTime.UtcNow.Ticks}",
                Price = plan.Price,
                Status = TransactionStatus.PENDING,
                Description = $"Payment for {plan.Name} subscription",
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.UserSubscriptionTransactionRepository.AddAsync(transaction);
            await _unitOfWork.SaveAsync();

            // Generate payment URL from PayOS
            var paymentLink = await _payOSService.CreatePaymentUrl((int)userSubscription.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Payment URL generated successfully. Please complete the payment to activate your subscription.",
                Data = new
                {
                    PaymentUrl = paymentLink.CheckoutUrl,
                    Amount = plan.Price,
                    SubscriptionPlanName = plan.Name,
                    UserSubscriptionId = userSubscription.Id,
                    TransactionId = transaction.Id,
                    ExpiredAt = paymentLink.ExpiredAt
                }
            };
        }

        public async Task<BaseResponseModel> GetMySubscriptionAsync(long userId)
        {
            var subscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = "No active subscription found",
                    Data = null
                };
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Active subscription retrieved successfully",
                Data = _mapper.Map<UserSubscriptionModel>(subscription)
            };
        }

        public async Task<BaseResponseModel> GetAvailablePlansAsync()
        {
            var plans = (await _unitOfWork.SubscriptionPlanRepository.GetAllAsync())
                .Where(p => p.Status == SubscriptionPlanStatus.ACTIVE)
                .ToList();
            var result = _mapper.Map<IEnumerable<SubscriptionPlanModel>>(plans);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Available subscription plans retrieved successfully",
                Data = result
            };
        }

        public async Task<BaseResponseModel> GetSubscriptionHistoryAsync(long userId)
        {
            var subscriptions = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Subscription history retrieved successfully",
                Data = _mapper.Map<IEnumerable<UserSubscriptionModel>>(subscriptions)
            };
        }

        public async Task<BaseResponseModel> ProcessPaymentWebhookAsync(long transactionId, string paymentStatus)
        {
            // Find the transaction
            var transaction = await _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                .Include(t => t.UserSubscription)
                    .ThenInclude(us => us.SubscriptionPlan)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new NotFoundException("Transaction not found");

            // Check if transaction is already processed
            if (transaction.Status != TransactionStatus.PENDING)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = $"Transaction already processed with status: {transaction.Status}",
                    Data = null
                };
            }

            // Process based on payment status from PayOS webhook
            if (paymentStatus == "PAID" || paymentStatus == "00") // PayOS success codes
            {
                // Update transaction status to COMPLETED
                transaction.Status = TransactionStatus.COMPLETED;
                transaction.UpdatedDate = DateTime.UtcNow;

                // Activate the subscription
                transaction.UserSubscription.IsActive = true;
                transaction.UserSubscription.UpdatedDate = DateTime.UtcNow;

                _unitOfWork.UserSubscriptionTransactionRepository.UpdateAsync(transaction);
                _unitOfWork.UserSubscriptionRepository.UpdateAsync(transaction.UserSubscription);
                await _unitOfWork.SaveAsync();

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Payment successful. Subscription activated.",
                    Data = new
                    {
                        TransactionId = transaction.Id,
                        Status = transaction.Status.ToString(),
                        UserSubscriptionId = transaction.UserSubscriptionId,
                        IsActive = transaction.UserSubscription.IsActive
                    }
                };
            }
            else // Payment failed or cancelled
            {
                // Update transaction status to FAILED
                transaction.Status = TransactionStatus.FAILED;
                transaction.UpdatedDate = DateTime.UtcNow;

                // Keep subscription inactive
                transaction.UserSubscription.IsActive = false;
                transaction.UserSubscription.UpdatedDate = DateTime.UtcNow;

                _unitOfWork.UserSubscriptionTransactionRepository.UpdateAsync(transaction);
                _unitOfWork.UserSubscriptionRepository.UpdateAsync(transaction.UserSubscription);
                await _unitOfWork.SaveAsync();

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Payment failed. Subscription not activated.",
                    Data = new
                    {
                        TransactionId = transaction.Id,
                        Status = transaction.Status.ToString(),
                        UserSubscriptionId = transaction.UserSubscriptionId,
                        IsActive = transaction.UserSubscription.IsActive
                    }
                };
            }
        }

        private List<Benefit> DeserializeBenefitLimit(string? jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return new List<Benefit>();

            try
            {
                return JsonSerializer.Deserialize<List<Benefit>>(jsonString) ?? new List<Benefit>();
            }
            catch
            {
                return new List<Benefit>();
            }
        }
    }
}
