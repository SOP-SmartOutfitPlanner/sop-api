using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.SubscriptionLimitModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class SubscriptionLimitService : ISubscriptionLimitService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SubscriptionLimitService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> CanPerformActionAsync(long userId, string usageKey, string limitKey)
        {
            var userSubscription = await GetActiveUserSubscriptionAsync(userId);

            // If user has no subscription, deny access
            if (userSubscription == null)
            {
                return false;
            }

            // Parse benefit limit
            var benefitLimit = ParseBenefitLimit(userSubscription.SubscriptionPlan.BenefitLimit);
            var limit = benefitLimit?.GetLimitValue(limitKey);

            // If limit is null, it means unlimited
            if (limit == null)
            {
                return true;
            }

            // Parse current usage
            var benefitUsage = ParseBenefitUsage(userSubscription.BenefitUsed);
            var currentUsage = benefitUsage?.GetUsageValue(usageKey) ?? 0;

            // Check if current usage is less than limit
            return currentUsage < limit.Value;
        }

        public async Task<bool> HasFeatureAccessAsync(long userId, string featureName)
        {
            var userSubscription = await GetActiveUserSubscriptionAsync(userId);

            // If user has no subscription, deny access
            if (userSubscription == null)
            {
                return false;
            }

            // Parse benefit limit
            var benefitLimit = ParseBenefitLimit(userSubscription.SubscriptionPlan.BenefitLimit);

            return benefitLimit?.HasFeature(featureName) ?? false;
        }

        public async Task IncrementUsageAsync(long userId, string usageKey, int amount = 1)
        {
            var userSubscription = await GetActiveUserSubscriptionAsync(userId);

            if (userSubscription == null)
            {
                return;
            }

            // Parse current usage
            var benefitUsage = ParseBenefitUsage(userSubscription.BenefitUsed) ?? new SubscriptionBenefitUsage();

            // Increment usage
            benefitUsage.IncrementUsage(usageKey, amount);

            // Save back to database
            userSubscription.BenefitUsed = JsonConvert.SerializeObject(benefitUsage);
            _unitOfWork.UserSubscriptionRepository.UpdateAsync(userSubscription);
            await _unitOfWork.SaveAsync();
        }

        public async Task DecrementUsageAsync(long userId, string usageKey, int amount = 1)
        {
            var userSubscription = await GetActiveUserSubscriptionAsync(userId);

            if (userSubscription == null)
            {
                return;
            }

            // Parse current usage
            var benefitUsage = ParseBenefitUsage(userSubscription.BenefitUsed) ?? new SubscriptionBenefitUsage();

            // Decrement usage
            benefitUsage.DecrementUsage(usageKey, amount);

            // Save back to database
            userSubscription.BenefitUsed = JsonConvert.SerializeObject(benefitUsage);
            _unitOfWork.UserSubscriptionRepository.UpdateAsync(userSubscription);
            await _unitOfWork.SaveAsync();
        }

        public async Task<(int currentUsage, int? limit)> GetUsageInfoAsync(long userId, string usageKey, string limitKey)
        {
            var userSubscription = await GetActiveUserSubscriptionAsync(userId);

            if (userSubscription == null)
            {
                return (0, 0);
            }

            var benefitLimit = ParseBenefitLimit(userSubscription.SubscriptionPlan.BenefitLimit);
            var benefitUsage = ParseBenefitUsage(userSubscription.BenefitUsed);

            var currentUsage = benefitUsage?.GetUsageValue(usageKey) ?? 0;
            var limit = benefitLimit?.GetLimitValue(limitKey);

            return (currentUsage, limit);
        }

        private async Task<UserSubscription?> GetActiveUserSubscriptionAsync(long userId)
        {
            return await _unitOfWork.UserSubscriptionRepository
                .GetQueryable()
                .Where(us => us.UserId == userId && us.IsActive && us.DateExp > DateTime.UtcNow)
                .Include(us => us.SubscriptionPlan)
                .OrderByDescending(us => us.DateExp)
                .FirstOrDefaultAsync();
        }

        private SubscriptionBenefitLimit? ParseBenefitLimit(string? benefitLimitJson)
        {
            if (string.IsNullOrWhiteSpace(benefitLimitJson))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<SubscriptionBenefitLimit>(benefitLimitJson);
            }
            catch
            {
                return null;
            }
        }

        private SubscriptionBenefitUsage? ParseBenefitUsage(string? benefitUsedJson)
        {
            if (string.IsNullOrWhiteSpace(benefitUsedJson))
            {
                return new SubscriptionBenefitUsage();
            }

            try
            {
                return JsonConvert.DeserializeObject<SubscriptionBenefitUsage>(benefitUsedJson);
            }
            catch
            {
                return new SubscriptionBenefitUsage();
            }
        }
    }
}
