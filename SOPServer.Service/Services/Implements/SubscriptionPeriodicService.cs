using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Repository.Utils;
using SOPServer.Service.BusinessModels.SubscriptionLimitModels;
using SOPServer.Service.Services.Interfaces;
using System.Text.Json;

namespace SOPServer.Service.Services.Implements
{
    public class SubscriptionPeriodicService : BackgroundService, ISubscriptionPeriodicService
    {
        private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(10));
        private readonly IServiceScopeFactory _scopeFactory;

        public SubscriptionPeriodicService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task CheckAndDeactivateExpiredSubscriptionsAsync()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();

                    var currentTime = CommonUtils.GetCurrentTime();

                    var freePlan = (await unitOfWork.SubscriptionPlanRepository.GetAllAsync())
                        .FirstOrDefault(p => p.Price == 0 && p.Status == SubscriptionPlanStatus.ACTIVE);

                    var expiredSubscriptions = await unitOfWork.UserSubscriptionRepository.GetQueryable()
                        .Include(s => s.SubscriptionPlan)
                        .Where(s => s.IsActive && s.DateExp <= currentTime)
                        .ToListAsync();

                    var expiredUserIds = new HashSet<long>();

                    foreach (var subscription in expiredSubscriptions)
                    {
                        subscription.IsActive = false;
                        subscription.UpdatedDate = currentTime;
                        unitOfWork.UserSubscriptionRepository.UpdateAsync(subscription);
                        expiredUserIds.Add(subscription.UserId);

                        Console.WriteLine($"[SubscriptionPeriodicService] Deactivated expired subscription: UserId={subscription.UserId}, PlanId={subscription.SubscriptionPlanId}, DateExp={subscription.DateExp}");
                    }

                    if (expiredSubscriptions.Any())
                    {
                        await unitOfWork.SaveAsync();
                    }

                    // Process users with expired subscriptions
                    foreach (var userId in expiredUserIds)
                    {
                        await ProcessUserSubscriptionAsync(unitOfWork, redisService, freePlan, userId, currentTime);
                    }

                    if (expiredUserIds.Any())
                    {
                        await unitOfWork.SaveAsync();
                        Console.WriteLine($"[SubscriptionPeriodicService] Processed {expiredSubscriptions.Count} expired subscriptions for {expiredUserIds.Count} users");
                    }

                    await AssignFreePlanToNewUsersAsync(unitOfWork, redisService, freePlan, currentTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SubscriptionPeriodicService] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Process a single user: reset IsPremium if needed and create free plan if no active subscription
        /// </summary>
        private async Task ProcessUserSubscriptionAsync(
            IUnitOfWork unitOfWork,
            IRedisService redisService,
            SubscriptionPlan? freePlan,
            long userId,
            DateTime currentTime)
        {
            var hasActivePaidSubscription = await unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .AnyAsync(s => s.UserId == userId && s.IsActive && s.SubscriptionPlan.Price > 0);

            var hasActiveSubscription = await unitOfWork.UserSubscriptionRepository.GetQueryable()
                .AnyAsync(s => s.UserId == userId && s.IsActive && s.DateExp > currentTime);

            if (!hasActivePaidSubscription)
            {
                var user = await unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user != null && user.IsPremium)
                {
                    user.IsPremium = false;
                    user.UpdatedDate = currentTime;
                    unitOfWork.UserRepository.UpdateAsync(user);

                    Console.WriteLine($"[SubscriptionPeriodicService] Set IsPremium=false for UserId={userId}");
                }
            }

            // Create free plan if user has no active subscription
            if (!hasActiveSubscription && freePlan != null)
            {
                await CreateFreePlanForUserAsync(unitOfWork, freePlan, userId, currentTime);
            }

            // Clear Redis cache
            var cacheKey = $"user_subscription_check:{userId}";
            await redisService.RemoveAsync(cacheKey);
        }

        /// <summary>
        /// Find all users without any subscription and assign free plan to them
        /// </summary>
        private async Task AssignFreePlanToNewUsersAsync(
            IUnitOfWork unitOfWork,
            IRedisService redisService,
            SubscriptionPlan? freePlan,
            DateTime currentTime)
        {
            if (freePlan == null)
            {
                return;
            }

            var usersWithSubscription = await unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            var usersWithoutSubscription = await unitOfWork.UserRepository.GetQueryable()
                .Where(u => !u.IsDeleted && u.Role != Role.ADMIN && !usersWithSubscription.Contains(u.Id))
                .ToListAsync();

            if (!usersWithoutSubscription.Any())
            {
                return;
            }

            foreach (var user in usersWithoutSubscription)
            {
                var hasPendingTransaction = await unitOfWork.UserSubscriptionTransactionRepository.GetQueryable()
                    .Include(t => t.UserSubscription)
                    .AnyAsync(t => t.UserSubscription.UserId == user.Id && t.Status == TransactionStatus.PENDING);

                if (hasPendingTransaction)
                {
                    continue;
                }

                await CreateFreePlanForUserAsync(unitOfWork, freePlan, user.Id, currentTime);

                // Clear Redis cache
                var cacheKey = $"user_subscription_check:{user.Id}";
                await redisService.RemoveAsync(cacheKey);
            }

            await unitOfWork.SaveAsync();

            if (usersWithoutSubscription.Any())
            {
                Console.WriteLine($"[SubscriptionPeriodicService] Assigned free plan to {usersWithoutSubscription.Count} new users");
            }
        }

        /// <summary>
        /// Create a free plan subscription for a user
        /// </summary>
        private async Task CreateFreePlanForUserAsync(
            IUnitOfWork unitOfWork,
            SubscriptionPlan freePlan,
            long userId,
            DateTime currentTime)
        {
            var planBenefits = DeserializeBenefitLimit(freePlan.BenefitLimit);

            var newSubscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = freePlan.Id,
                DateExp = currentTime.AddMonths(1),
                IsActive = true,
                BenefitUsed = JsonSerializer.Serialize(planBenefits)
            };

            await unitOfWork.UserSubscriptionRepository.AddAsync(newSubscription);

            Console.WriteLine($"[SubscriptionPeriodicService] Created free plan for UserId={userId}");
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //await CheckAndDeactivateExpiredSubscriptionsAsync();

                while (await _timer.WaitForNextTickAsync(stoppingToken))
                {
                    await CheckAndDeactivateExpiredSubscriptionsAsync();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
