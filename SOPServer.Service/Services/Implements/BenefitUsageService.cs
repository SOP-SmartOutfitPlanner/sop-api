using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.SubscriptionLimitModels;
using SOPServer.Service.Services.Interfaces;
using System.Text.Json;

namespace SOPServer.Service.Services.Implements
{
    public class BenefitUsageService : IBenefitUsageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BenefitUsageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> CanUseFeatureAsync(long userId, FeatureCode featureCode)
        {
            var subscription = await GetActiveSubscriptionAsync(userId);
            if (subscription == null) return false;

            var benefitUsage = DeserializeBenefitUsage(subscription.BenefitUsed);
            var benefit = benefitUsage.FirstOrDefault(b => b.FeatureCode == featureCode);

            if (benefit == null) return false;

            // Check if user has remaining credits
            return benefit.Usage > 0;
        }

        public async Task<bool> DecrementUsageAsync(long userId, FeatureCode featureCode, int amount = 1)
        {
            var subscription = await GetActiveSubscriptionAsync(userId);
            if (subscription == null) return false;

            var benefitUsage = DeserializeBenefitUsage(subscription.BenefitUsed);
            var benefit = benefitUsage.FirstOrDefault(b => b.FeatureCode == featureCode);

            if (benefit == null || benefit.Usage < amount) return false;

            // Decrement credits
            benefit.Usage -= amount;

            // Save back to database
            subscription.BenefitUsed = JsonSerializer.Serialize(benefitUsage);
            _unitOfWork.UserSubscriptionRepository.UpdateAsync(subscription);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<bool> IncrementUsageAsync(long userId, FeatureCode featureCode, int amount = 1)
        {   
            var subscription = await GetActiveSubscriptionAsync(userId);
            if (subscription == null) return false;

            var benefitUsage = DeserializeBenefitUsage(subscription.BenefitUsed);
            var benefit = benefitUsage.FirstOrDefault(b => b.FeatureCode == featureCode);

            if (benefit == null) return false;

            // Only increment for BenefitType.Persistent (credit-based features like wardrobe items)
            if (benefit.BenefitType != BenefitType.Persistent) return false;

            // Get the plan limit to prevent going over the maximum
            var planBenefitLimit = DeserializeBenefitLimit(subscription.SubscriptionPlan.BenefitLimit);
            var planBenefit = planBenefitLimit.FirstOrDefault(b => b.FeatureCode == featureCode);

            if (planBenefit == null) return false;

            // Increment credits but don't exceed the plan limit
            benefit.Usage = Math.Min(benefit.Usage + amount, planBenefit.Usage);

            // Save back to database
            subscription.BenefitUsed = JsonSerializer.Serialize(benefitUsage);
            _unitOfWork.UserSubscriptionRepository.UpdateAsync(subscription);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<(int remainingCredits, int? totalLimit)> GetUsageInfoAsync(long userId, FeatureCode featureCode)
        {
            var subscription = await GetActiveSubscriptionAsync(userId);
            if (subscription == null) return (0, null);

            var benefitUsage = DeserializeBenefitUsage(subscription.BenefitUsed);
            var benefit = benefitUsage.FirstOrDefault(b => b.FeatureCode == featureCode);

            if (benefit == null) return (0, null);

            var planBenefitLimit = DeserializeBenefitLimit(subscription.SubscriptionPlan.BenefitLimit);
            var planBenefit = planBenefitLimit.FirstOrDefault(b => b.FeatureCode == featureCode);

            return (benefit.Usage, planBenefit?.Usage);
        }

        private async Task<Repository.Entities.UserSubscription?> GetActiveSubscriptionAsync(long userId)
        {
            return await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefaultAsync();
        }

        private List<Benefit> DeserializeBenefitUsage(string? jsonString)
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
