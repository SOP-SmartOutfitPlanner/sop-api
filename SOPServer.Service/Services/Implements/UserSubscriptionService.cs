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
        private readonly IRedisService _redisService;

        public UserSubscriptionService(IUnitOfWork unitOfWork, IMapper mapper, IPayOSService payOSService, IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _payOSService = payOSService;
            _redisService = redisService;
        }

        public async Task<BaseResponseModel> PurchaseSubscriptionAsync(long userId, PurchaseSubscriptionRequestModel model)
        {
            // Get the subscription plan
            var plan = await _unitOfWork.SubscriptionPlanRepository.GetByIdAsync(model.SubscriptionPlanId);
            if (plan == null)
                throw new NotFoundException(MessageConstants.SUBSCRIPTION_PLAN_NOT_FOUND);

            // Check plan status and user eligibility
            if (plan.Status == SubscriptionPlanStatus.DRAFT)
                throw new BadRequestException(MessageConstants.SUBSCRIPTION_PLAN_DRAFT);

            if (plan.Status == SubscriptionPlanStatus.ARCHIVED)
                throw new BadRequestException(MessageConstants.SUBSCRIPTION_PLAN_ARCHIVED);

            // For INACTIVE plans, only users who previously had this plan can rebuy
            if (plan.Status == SubscriptionPlanStatus.INACTIVE)
            {
                var hasPreviousSubscription = (await _unitOfWork.UserSubscriptionRepository.GetAllAsync())
                    .Any(s => s.UserId == userId && s.SubscriptionPlanId == plan.Id);

                if (!hasPreviousSubscription)
                    throw new BadRequestException(MessageConstants.SUBSCRIPTION_PLAN_INACTIVE_CUSTOMERS_ONLY);
            }

            // Check if user already has an active subscription
            var existingSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (existingSubscription != null)
            {
                // Allow upgrade from free to paid
                if (existingSubscription.SubscriptionPlan.Price > 0)
                    throw new BadRequestException(string.Format(MessageConstants.USER_SUBSCRIPTION_ALREADY_HAS_PAID,
                        existingSubscription.SubscriptionPlan.Name,
                        existingSubscription.DateExp.ToString("dd-MM-yyyy HH:mm")));

                // Deactivate free plan to allow upgrade to paid plan
                existingSubscription.IsActive = false;
                existingSubscription.UpdatedDate = DateTime.UtcNow;
                _unitOfWork.UserSubscriptionRepository.UpdateAsync(existingSubscription);
                await _unitOfWork.SaveAsync();
            }

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
                    // User already has a pending payment, return existing payment QR code
                    var existingPaymentLink = await _payOSService.CreatePaymentUrl((int)pendingSubscription.Id);
                    return new BaseResponseModel
                    {
                        StatusCode = StatusCodes.Status200OK,
                        Message = MessageConstants.USER_SUBSCRIPTION_PENDING_PAYMENT,
                        Data = new
                        {
                            QrCode = existingPaymentLink.QrCode,
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

            // Calculate initial benefits (carry over Persistent, reset Renewable)
            var initialBenefits = await CalculateInitialBenefitsAsync(userId, plan.Id, planBenefits);

            // Handle FREE plan differently - activate immediately without payment
            if (plan.Price == 0)
            {
                // Create active subscription immediately for free plans
                var freeSubscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionPlanId = plan.Id,
                    DateExp = DateTime.UtcNow.AddMonths(1), // 1 month subscription
                    IsActive = true, // Activate immediately for free plans
                    BenefitUsed = JsonSerializer.Serialize(initialBenefits),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                await _unitOfWork.UserSubscriptionRepository.AddAsync(freeSubscription);
                await _unitOfWork.SaveAsync();

                // Create completed transaction record for free plan
                var freeTransaction = new UserSubscriptionTransaction
                {
                    UserSubscriptionId = freeSubscription.Id,
                    TransactionCode = $"FREE-{DateTime.UtcNow.Ticks}",
                    Price = 0,
                    Status = TransactionStatus.COMPLETED,
                    Description = string.Format(MessageConstants.USER_SUBSCRIPTION_FREE_DESCRIPTION, plan.Name),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                await _unitOfWork.UserSubscriptionTransactionRepository.AddAsync(freeTransaction);
                await _unitOfWork.SaveAsync();

                // Clear cache to ensure middleware picks up new subscription
                var cacheKey = $"user_subscription_check:{userId}";
                await _redisService.RemoveAsync(cacheKey);

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.USER_SUBSCRIPTION_FREE_ACTIVATED,
                    Data = new
                    {
                        SubscriptionPlanName = plan.Name,
                        UserSubscriptionId = freeSubscription.Id,
                        TransactionId = freeTransaction.Id,
                        DateExp = freeSubscription.DateExp,
                        IsActive = true,
                        BenefitUsage = initialBenefits
                    }
                };
            }

            // For PAID plans, create pending subscription and payment flow
            var userSubscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                DateExp = DateTime.UtcNow.AddMonths(1), // 1 month subscription
                IsActive = false, // Will be activated after payment
                BenefitUsed = JsonSerializer.Serialize(initialBenefits), // Smart merge: carry over Persistent, reset Renewable
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
                Description = string.Format(MessageConstants.USER_SUBSCRIPTION_PAYMENT_DESCRIPTION, plan.Name),
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.UserSubscriptionTransactionRepository.AddAsync(transaction);
            await _unitOfWork.SaveAsync();

            // Generate payment QR code from PayOS
            var paymentLink = await _payOSService.CreatePaymentUrl((int)userSubscription.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_SUBSCRIPTION_PAYMENT_QR_GENERATED,
                Data = new
                {
                    QrCode = paymentLink.QrCode,
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
                    Message = MessageConstants.USER_SUBSCRIPTION_NO_ACTIVE,
                    Data = null
                };
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_SUBSCRIPTION_GET_ACTIVE_SUCCESS,
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
                Message = MessageConstants.USER_SUBSCRIPTION_GET_AVAILABLE_PLANS_SUCCESS,
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
                Message = MessageConstants.USER_SUBSCRIPTION_GET_HISTORY_SUCCESS,
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
                throw new NotFoundException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_NOT_FOUND);

            // Check if transaction is already processed
            if (transaction.Status != TransactionStatus.PENDING)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = string.Format(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_ALREADY_PROCESSED, transaction.Status),
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

                // Clear Redis cache to ensure middleware picks up activated subscription
                var cacheKey = $"user_subscription_check:{transaction.UserSubscription.UserId}";
                await _redisService.RemoveAsync(cacheKey);

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.USER_SUBSCRIPTION_PAYMENT_SUCCESS,
                    Data = new
                    {
                        UserId = transaction.UserSubscription.UserId,
                        TransactionId = transaction.Id,
                        Status = transaction.Status.ToString(),
                        UserSubscriptionId = transaction.UserSubscriptionId,
                        IsActive = transaction.UserSubscription.IsActive,
                        DateExp = transaction.UserSubscription.DateExp,
                        SubscriptionPlanName = transaction.UserSubscription.SubscriptionPlan.Name
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
                    Message = MessageConstants.USER_SUBSCRIPTION_PAYMENT_FAILED,
                    Data = new
                    {
                        UserId = transaction.UserSubscription.UserId,
                        TransactionId = transaction.Id,
                        Status = transaction.Status.ToString(),
                        UserSubscriptionId = transaction.UserSubscriptionId,
                        IsActive = transaction.UserSubscription.IsActive,
                        SubscriptionPlanName = transaction.UserSubscription.SubscriptionPlan.Name
                    }
                };
            }
        }

        public async Task EnsureUserHasActiveSubscriptionAsync(long userId)
        {
            // Check cache first to avoid DB query on every request
            var cacheKey = $"user_subscription_check:{userId}";
            var hasCachedSubscription = await _redisService.ExistsAsync(cacheKey);

            if (hasCachedSubscription)
                return; // User has active subscription (cached)

            // Check database for active subscription
            var activeSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (activeSubscription != null)
            {
                // User has active subscription, cache it for 5 minutes
                await _redisService.SetAsync(cacheKey, true, TimeSpan.FromMinutes(5));
                return;
            }

            // User has no active subscription - auto-create free plan
            var freePlan = (await _unitOfWork.SubscriptionPlanRepository.GetAllAsync())
                .FirstOrDefault(p => p.Price == 0 && p.Status == SubscriptionPlanStatus.ACTIVE);

            if (freePlan == null)
            {
                // No free plan exists, user cannot use the app
                // This should never happen in production - free plan should always exist
                return;
            }

            // Deserialize free plan benefits
            var planBenefits = DeserializeBenefitLimit(freePlan.BenefitLimit);

            // Create new free subscription
            var newSubscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = freePlan.Id,
                DateExp = DateTime.UtcNow.AddMonths(1), // 1 month free subscription
                IsActive = true,
                BenefitUsed = JsonSerializer.Serialize(planBenefits), // Fresh credits
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            await _unitOfWork.UserSubscriptionRepository.AddAsync(newSubscription);
            await _unitOfWork.SaveAsync();

            // Cache the subscription status for 5 minutes
            await _redisService.SetAsync(cacheKey, true, TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Merges previous subscription benefits with new plan benefits.
        /// - BenefitType.Persistent: Carries over remaining credits from previous subscription
        /// - BenefitType.Renewable: Resets to fresh credits from plan
        /// </summary>
        private async Task<List<Benefit>> CalculateInitialBenefitsAsync(long userId, long subscriptionPlanId, List<Benefit> planBenefits)
        {
            // Get the user's most recent expired subscription for the SAME plan
            var previousSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Where(s => s.UserId == userId &&
                            s.SubscriptionPlanId == subscriptionPlanId &&
                            s.DateExp <= DateTime.UtcNow) // Expired subscriptions only
                .OrderByDescending(s => s.DateExp)
                .FirstOrDefaultAsync();

            // If no previous subscription, return fresh credits from plan
            if (previousSubscription == null || string.IsNullOrEmpty(previousSubscription.BenefitUsed))
            {
                return planBenefits;
            }

            // Deserialize previous subscription's benefits
            var previousBenefits = DeserializeBenefitLimit(previousSubscription.BenefitUsed);

            // Merge logic
            var mergedBenefits = new List<Benefit>();

            foreach (var planBenefit in planBenefits)
            {
                var previousBenefit = previousBenefits.FirstOrDefault(b => b.FeatureCode == planBenefit.FeatureCode);

                if (previousBenefit == null)
                {
                    // New feature in plan, use plan's limit
                    mergedBenefits.Add(new Benefit
                    {
                        FeatureCode = planBenefit.FeatureCode,
                        Usage = planBenefit.Usage,
                        BenefitType = planBenefit.BenefitType
                    });
                }
                else if (previousBenefit.BenefitType == BenefitType.Persistent)
                {
                    // Persistent feature - carry over remaining credits
                    // But don't exceed the plan limit if plan was upgraded
                    mergedBenefits.Add(new Benefit
                    {
                        FeatureCode = planBenefit.FeatureCode,
                        Usage = Math.Min(previousBenefit.Usage, planBenefit.Usage),
                        BenefitType = BenefitType.Persistent
                    });
                }
                else // BenefitType.Renewable
                {
                    // Renewable feature - reset to fresh credits
                    mergedBenefits.Add(new Benefit
                    {
                        FeatureCode = planBenefit.FeatureCode,
                        Usage = planBenefit.Usage,
                        BenefitType = BenefitType.Renewable
                    });
                }
            }

            return mergedBenefits;
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
