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
            // Acquire distributed lock to prevent concurrent purchase requests from same user
            var purchaseLockKey = $"purchase_lock:{userId}";
            var lockAcquired = await _redisService.AcquireLockAsync(purchaseLockKey, TimeSpan.FromSeconds(30));

            if (!lockAcquired)
            {
                // Another purchase request is in progress, wait a bit and check cache
                await Task.Delay(500);
            }

            try
            {
                // Check Redis cache for existing pending payment response
                var purchaseCacheKey = $"purchase_subscription_pending:{userId}";
                var (cachedData, remainingTtl) = await _redisService.GetWithTtlAsync<PendingPaymentCacheModel>(purchaseCacheKey);
                if (cachedData != null && remainingTtl.HasValue && remainingTtl.Value > TimeSpan.Zero)
                {
                    return new BaseResponseModel
                    {
                        StatusCode = StatusCodes.Status200OK,
                        Message = MessageConstants.USER_SUBSCRIPTION_PENDING_PAYMENT,
                        Data = new
                        {
                            cachedData.QrCode,
                            cachedData.PaymentUrl,
                            cachedData.Amount,
                            cachedData.SubscriptionPlanName,
                            cachedData.UserSubscriptionId,
                            cachedData.TransactionId,
                            cachedData.Description,
                            cachedData.ExpiredAt,
                            cachedData.BankInfo
                        }
                    };
                }

                // Check if user has any pending transactions (double-check after lock acquired)
                var existingPendingTransaction = await _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                    .Include(t => t.UserSubscription)
                        .ThenInclude(us => us.SubscriptionPlan)
                    .Where(t => t.UserSubscription.UserId == userId && t.Status == TransactionStatus.PENDING)
                    .OrderByDescending(t => t.CreatedDate)
                    .FirstOrDefaultAsync();

                if (existingPendingTransaction != null)
                {
                    throw new BadRequestException($"You already have a pending payment for {existingPendingTransaction.UserSubscription.SubscriptionPlan.Name}. Please complete or cancel that payment first.");
                }

            var plan = await _unitOfWork.SubscriptionPlanRepository.GetByIdAsync(model.SubscriptionPlanId);
            if (plan == null)
                throw new NotFoundException(MessageConstants.SUBSCRIPTION_PLAN_NOT_FOUND);

            if (plan.Status == SubscriptionPlanStatus.DRAFT)
                throw new BadRequestException(MessageConstants.SUBSCRIPTION_PLAN_DRAFT);

            if (plan.Status == SubscriptionPlanStatus.ARCHIVED)
                throw new BadRequestException(MessageConstants.SUBSCRIPTION_PLAN_ARCHIVED);

            if (plan.Status == SubscriptionPlanStatus.INACTIVE)
            {
                var hasPreviousSubscription = (await _unitOfWork.UserSubscriptionRepository.GetAllAsync())
                    .Any(s => s.UserId == userId && s.SubscriptionPlanId == plan.Id);

                if (!hasPreviousSubscription)
                    throw new BadRequestException(MessageConstants.SUBSCRIPTION_PLAN_INACTIVE_CUSTOMERS_ONLY);
            }

            var existingActiveSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow && s.SubscriptionPlan.Price > 0)
                .FirstOrDefaultAsync();

            if (existingActiveSubscription != null)
            {
                throw new BadRequestException(string.Format(MessageConstants.USER_SUBSCRIPTION_ALREADY_HAS_PAID,
                    existingActiveSubscription.SubscriptionPlan.Name,
                    existingActiveSubscription.DateExp.ToString("dd-MM-yyyy HH:mm")));
            }


            var pendingTransactions = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(us => us.UserSubscriptionTransactions)
                .Where(us => us.UserId == userId)
                .SelectMany(us => us.UserSubscriptionTransactions)
                .Where(t => t.Status == TransactionStatus.PENDING)
                .ToListAsync();

            foreach (var pt in pendingTransactions)
            {
                pt.Status = TransactionStatus.FAILED;
                pt.UpdatedDate = DateTime.UtcNow;
                _unitOfWork.UserSubscriptionTransactionRepository.UpdateAsync(pt);
            }
            if (pendingTransactions.Any())
            {
                await _unitOfWork.SaveAsync();
            }

            var planBenefits = DeserializeBenefitLimit(plan.BenefitLimit);

            var initialBenefits = await CalculateInitialBenefitsAsync(userId, plan.Id, planBenefits);

            if (plan.Price == 0)
            {
                var freeSubscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionPlanId = plan.Id,
                    DateExp = DateTime.UtcNow.AddMonths(1), 
                    IsActive = true, 
                    BenefitUsed = JsonSerializer.Serialize(initialBenefits),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                await _unitOfWork.UserSubscriptionRepository.AddAsync(freeSubscription);
                await _unitOfWork.SaveAsync();

                var freeTransaction = new UserSubscriptionTransaction
                {
                    UserSubscriptionId = freeSubscription.Id,
                    TransactionCode = GenerateTransactionCode(),
                    Price = 0,
                    Status = TransactionStatus.COMPLETED,
                    Description = string.Format(MessageConstants.USER_SUBSCRIPTION_FREE_DESCRIPTION, plan.Name),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                await _unitOfWork.UserSubscriptionTransactionRepository.AddAsync(freeTransaction);
                await _unitOfWork.SaveAsync();

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

            var userSubscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                DateExp = DateTime.UtcNow.AddMonths(1), 
                IsActive = false,
                BenefitUsed = JsonSerializer.Serialize(initialBenefits),
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            await _unitOfWork.UserSubscriptionRepository.AddAsync(userSubscription);
            await _unitOfWork.SaveAsync();

            var transaction = new UserSubscriptionTransaction
            {
                UserSubscriptionId = userSubscription.Id,
                TransactionCode = GenerateTransactionCode(),
                Price = plan.Price,
                Status = TransactionStatus.PENDING,
                Description = string.Format(MessageConstants.USER_SUBSCRIPTION_PAYMENT_DESCRIPTION, plan.Name),
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            await _unitOfWork.UserSubscriptionTransactionRepository.AddAsync(transaction);
            await _unitOfWork.SaveAsync();

            var paymentLink = await _payOSService.CreatePaymentUrl((int)userSubscription.Id);

            TimeSpan? ttl = null;
            if (paymentLink.ExpiredAt.HasValue)
            {
                var expiredAtDateTime = DateTimeOffset.FromUnixTimeSeconds(paymentLink.ExpiredAt.Value);
                ttl = expiredAtDateTime - DateTimeOffset.Now;
            }

            var paymentData = new PendingPaymentCacheModel
            {
                QrCode = paymentLink.QrCode,
                PaymentUrl = paymentLink.CheckoutUrl,
                Amount = plan.Price,
                SubscriptionPlanName = plan.Name,
                UserSubscriptionId = userSubscription.Id,
                TransactionId = transaction.Id,
                Description = MessageConstants.SUBSCRIPTION_TRANSACTION_DESCRIPTION + plan.Name,
                ExpiredAt = paymentLink.ExpiredAt,
                BankInfo = new BankInfoModel
                {
                    Bin = paymentLink.Bin,
                    AccountNumber = paymentLink.AccountNumber,
                    AccountName = paymentLink.AccountName
                }
            };

                if (ttl.HasValue && ttl.Value > TimeSpan.Zero)
                {
                    await _redisService.SetAsync(purchaseCacheKey, paymentData, ttl);
                }

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.USER_SUBSCRIPTION_PAYMENT_QR_GENERATED,
                    Data = new
                    {
                        paymentData.QrCode,
                        paymentData.PaymentUrl,
                        paymentData.Amount,
                        paymentData.SubscriptionPlanName,
                        paymentData.UserSubscriptionId,
                        paymentData.TransactionId,
                        paymentData.Description,
                        paymentData.ExpiredAt,
                        paymentData.BankInfo
                    }
                };
            }
            finally
            {
                if (lockAcquired)
                {
                    await _redisService.ReleaseLockAsync(purchaseLockKey);
                }
            }
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
                .Include(s => s.UserSubscriptionTransactions)
                .Where(s => s.UserId == userId && s.SubscriptionPlan.Price > 0)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_SUBSCRIPTION_GET_HISTORY_SUCCESS,
                Data = _mapper.Map<IEnumerable<UserSubscriptionModel>>(subscriptions)
            };
        }

        public async Task<BaseResponseModel> ProcessPaymentWebhookAsync(long transactionCode, string paymentStatus)
        {
            var transaction = await _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                .Include(t => t.UserSubscription)
                    .ThenInclude(us => us.SubscriptionPlan)
                .FirstOrDefaultAsync(t => t.TransactionCode == transactionCode);

            if (transaction == null)
                throw new NotFoundException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_NOT_FOUND);

            if (transaction.Status != TransactionStatus.PENDING)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = string.Format(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_ALREADY_PROCESSED, transaction.Status),
                    Data = null
                };
            }

            if (paymentStatus == "PAID" || paymentStatus == "00")
            {
                transaction.Status = TransactionStatus.COMPLETED;
                transaction.UpdatedDate = DateTime.UtcNow;

                transaction.UserSubscription.IsActive = true;
                transaction.UserSubscription.UpdatedDate = DateTime.UtcNow;

                if (transaction.UserSubscription.SubscriptionPlan.Price > 0)
                {
                    var activeFreeSubscriptions = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                        .Include(s => s.SubscriptionPlan)
                        .Where(s => s.UserId == transaction.UserSubscription.UserId
                                 && s.IsActive
                                 && s.SubscriptionPlan.Price == 0
                                 && s.Id != transaction.UserSubscriptionId)
                        .ToListAsync();

                    foreach (var freeSub in activeFreeSubscriptions)
                    {
                        freeSub.IsActive = false;
                        freeSub.UpdatedDate = DateTime.UtcNow;
                        _unitOfWork.UserSubscriptionRepository.UpdateAsync(freeSub);
                    }
                }

                // Load user for IsPremium update
                var user = await _unitOfWork.UserRepository.GetByIdAsync(transaction.UserSubscription.UserId);
                if (user != null && transaction.UserSubscription.SubscriptionPlan.Price > 0)
                {
                    user.IsPremium = true;
                    _unitOfWork.UserRepository.UpdateAsync(user);
                }

                _unitOfWork.UserSubscriptionTransactionRepository.UpdateAsync(transaction);
                _unitOfWork.UserSubscriptionRepository.UpdateAsync(transaction.UserSubscription);
                await _unitOfWork.SaveAsync();

                var cacheKey = $"user_subscription_check:{transaction.UserSubscription.UserId}";
                await _redisService.RemoveAsync(cacheKey);

                // Clear pending purchase cache
                var purchaseCacheKey = $"purchase_subscription_pending:{transaction.UserSubscription.UserId}";
                await _redisService.RemoveAsync(purchaseCacheKey);

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.USER_SUBSCRIPTION_PAYMENT_SUCCESS,
                    Data = new
                    {
                        UserId = transaction.UserSubscription.UserId,
                        TransactionId = transaction.Id,
                        TransactionCode = transaction.TransactionCode,
                        Status = transaction.Status.ToString(),
                        UserSubscriptionId = transaction.UserSubscriptionId,
                        IsActive = transaction.UserSubscription.IsActive,
                        DateExp = transaction.UserSubscription.DateExp,
                        SubscriptionPlanName = transaction.UserSubscription.SubscriptionPlan.Name
                    }
                };
            }
            else if (paymentStatus == "CANCELLED" || paymentStatus == "CANCELED")
            {
                transaction.Status = TransactionStatus.CANCELLED;
                transaction.UpdatedDate = DateTime.UtcNow;

                transaction.UserSubscription.IsActive = false;
                transaction.UserSubscription.UpdatedDate = DateTime.UtcNow;

                _unitOfWork.UserSubscriptionTransactionRepository.UpdateAsync(transaction);
                _unitOfWork.UserSubscriptionRepository.UpdateAsync(transaction.UserSubscription);
                await _unitOfWork.SaveAsync();

                // Clear pending purchase cache
                var cancelledPurchaseCacheKey = $"purchase_subscription_pending:{transaction.UserSubscription.UserId}";
                await _redisService.RemoveAsync(cancelledPurchaseCacheKey);

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.USER_SUBSCRIPTION_PAYMENT_CANCELLED,
                    Data = new
                    {
                        UserId = transaction.UserSubscription.UserId,
                        TransactionId = transaction.Id,
                        TransactionCode = transaction.TransactionCode,
                        Status = transaction.Status.ToString(),
                        UserSubscriptionId = transaction.UserSubscriptionId,
                        IsActive = transaction.UserSubscription.IsActive,
                        SubscriptionPlanName = transaction.UserSubscription.SubscriptionPlan.Name
                    }
                };
            }
            else
            {
                transaction.Status = TransactionStatus.FAILED;
                transaction.UpdatedDate = DateTime.UtcNow;

                transaction.UserSubscription.IsActive = false;
                transaction.UserSubscription.UpdatedDate = DateTime.UtcNow;

                _unitOfWork.UserSubscriptionTransactionRepository.UpdateAsync(transaction);
                _unitOfWork.UserSubscriptionRepository.UpdateAsync(transaction.UserSubscription);
                await _unitOfWork.SaveAsync();

                // Clear pending purchase cache
                var failedPurchaseCacheKey = $"purchase_subscription_pending:{transaction.UserSubscription.UserId}";
                await _redisService.RemoveAsync(failedPurchaseCacheKey);

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.USER_SUBSCRIPTION_PAYMENT_FAILED,
                    Data = new
                    {
                        UserId = transaction.UserSubscription.UserId,
                        TransactionId = transaction.Id,
                        TransactionCode = transaction.TransactionCode,
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
            var cacheKey = $"user_subscription_check:{userId}";
            var hasCachedSubscription = await _redisService.ExistsAsync(cacheKey);

            if (hasCachedSubscription)
                return;

            var activeSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (activeSubscription != null)
            {
                await _redisService.SetAsync(cacheKey, true, TimeSpan.FromMinutes(5));
                return;
            }

            var lockKey = $"create_subscription:{userId}";
            var lockAcquired = await _redisService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10));

            if (!lockAcquired)
            {
                await Task.Delay(100);
                return;
            }

            try
            {
                activeSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                    .Include(s => s.SubscriptionPlan)
                    .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (activeSubscription != null)
                {
                    await _redisService.SetAsync(cacheKey, true, TimeSpan.FromMinutes(5));
                    return;
                }

                var hasPendingTransactions = await _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                    .Include(t => t.UserSubscription)
                    .AnyAsync(t => t.UserSubscription.UserId == userId && t.Status == TransactionStatus.PENDING);

                if (hasPendingTransactions)
                {
                    return;
                }

                var anyRecentSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                    .Include(s => s.SubscriptionPlan)
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedDate)
                    .FirstOrDefaultAsync();

                if (anyRecentSubscription != null && anyRecentSubscription.DateExp > DateTime.UtcNow)
                {
                    return;
                }

                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user != null && user.IsPremium)
                {
                    var hasPaidSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                        .Include(s => s.SubscriptionPlan)
                        .AnyAsync(s => s.UserId == userId && s.IsActive && s.SubscriptionPlan.Price > 0);

                    if (!hasPaidSubscription)
                    {
                        user.IsPremium = false;
                        _unitOfWork.UserRepository.UpdateAsync(user);
                        await _unitOfWork.SaveAsync();
                    }
                }

                var freePlan = (await _unitOfWork.SubscriptionPlanRepository.GetAllAsync())
                    .FirstOrDefault(p => p.Price == 0 && p.Status == SubscriptionPlanStatus.ACTIVE);

                if (freePlan == null)
                {
                    return;
                }

                var planBenefits = DeserializeBenefitLimit(freePlan.BenefitLimit);
                var initialBenefits = await CalculateInitialBenefitsAsync(userId, freePlan.Id, planBenefits);

                var newSubscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionPlanId = freePlan.Id,
                    DateExp = DateTime.UtcNow.AddMonths(1),
                    IsActive = true,
                    BenefitUsed = JsonSerializer.Serialize(initialBenefits),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                await _unitOfWork.UserSubscriptionRepository.AddAsync(newSubscription);
                await _unitOfWork.SaveAsync();

                await _redisService.SetAsync(cacheKey, true, TimeSpan.FromMinutes(5));
            }
            finally
            {
                await _redisService.ReleaseLockAsync(lockKey);
            }
        }

        public async Task<BaseResponseModel> CancelPendingPaymentAsync(long userId, long transactionId)
        {
            var transaction = await _unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                .Include(t => t.UserSubscription)
                    .ThenInclude(us => us.SubscriptionPlan)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new NotFoundException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_NOT_FOUND);

            if (transaction.UserSubscription.UserId != userId)
                throw new UnauthorizedAccessException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_NOT_OWNED);

            if (transaction.Status != TransactionStatus.PENDING)
                throw new BadRequestException(MessageConstants.USER_SUBSCRIPTION_TRANSACTION_NOT_PENDING);

            try
            {
                await _payOSService.CancelPayment((int)transactionId, "Cancelled by user");
            }
            catch
            {
            }

            transaction.Status = TransactionStatus.CANCELLED;
            transaction.UpdatedDate = DateTime.UtcNow;

            transaction.UserSubscription.IsActive = false;
            transaction.UserSubscription.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.UserSubscriptionTransactionRepository.UpdateAsync(transaction);
            _unitOfWork.UserSubscriptionRepository.UpdateAsync(transaction.UserSubscription);
            await _unitOfWork.SaveAsync();

            var purchaseCacheKey = $"purchase_subscription_pending:{userId}";
            await _redisService.RemoveAsync(purchaseCacheKey);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_SUBSCRIPTION_PAYMENT_CANCELLED,
                Data = new
                {
                    TransactionId = transaction.Id,
                    Status = transaction.Status.ToString(),
                    SubscriptionPlanName = transaction.UserSubscription.SubscriptionPlan.Name
                }
            };
        }

        /// <summary>
        /// Merges previous subscription benefits with new plan benefits.
        /// - BenefitType.Persistent: Carries over remaining credits from previous subscription
        /// - BenefitType.Renewable: Resets to fresh credits from plan
        /// </summary>
        private async Task<List<Benefit>> CalculateInitialBenefitsAsync(long userId, long subscriptionPlanId, List<Benefit> planBenefits)
        {
            var previousSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Where(s => s.UserId == userId &&
                            s.SubscriptionPlanId == subscriptionPlanId &&
                            s.DateExp <= DateTime.UtcNow) // Expired subscriptions only
                .OrderByDescending(s => s.DateExp)
                .FirstOrDefaultAsync();

            if (previousSubscription == null || string.IsNullOrEmpty(previousSubscription.BenefitUsed))
            {
                return planBenefits;
            }

            var previousBenefits = DeserializeBenefitLimit(previousSubscription.BenefitUsed);

            var mergedBenefits = new List<Benefit>();

            foreach (var planBenefit in planBenefits)
            {
                var previousBenefit = previousBenefits.FirstOrDefault(b => b.FeatureCode == planBenefit.FeatureCode);

                if (previousBenefit == null)
                {
                    mergedBenefits.Add(new Benefit
                    {
                        FeatureCode = planBenefit.FeatureCode,
                        Usage = planBenefit.Usage,
                        BenefitType = planBenefit.BenefitType
                    });
                }
                else if (previousBenefit.BenefitType == BenefitType.Persistent)
                {
                    // Persistent feature 
                    mergedBenefits.Add(new Benefit
                    {
                        FeatureCode = planBenefit.FeatureCode,
                        Usage = Math.Min(previousBenefit.Usage, planBenefit.Usage),
                        BenefitType = BenefitType.Persistent
                    });
                }
                else 
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

        private int GenerateTransactionCode()
        {
            int timestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var random = new Random();
            int randomComponent = random.Next(100, 999);
            int transactionCode = Math.Abs((timestamp * 1000 + randomComponent) % int.MaxValue);
            return transactionCode;
        }
    }
}
