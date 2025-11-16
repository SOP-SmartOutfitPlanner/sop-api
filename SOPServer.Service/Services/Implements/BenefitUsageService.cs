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

            // Reset monthly benefits if needed
            await ResetMonthlyBenefitsIfNeededAsync(userId);

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

            // Reset monthly benefits if needed before decrementing
            await ResetMonthlyBenefitsIfNeededAsync(userId);

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

            // Only increment for ResetType.Never (credit-based features like wardrobe items)
            if (benefit.ResetType != ResetType.Never) return false;

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

            // Reset monthly benefits if needed
            await ResetMonthlyBenefitsIfNeededAsync(userId);

            var benefitUsage = DeserializeBenefitUsage(subscription.BenefitUsed);
            var benefit = benefitUsage.FirstOrDefault(b => b.FeatureCode == featureCode);

            if (benefit == null) return (0, null);

            var planBenefitLimit = DeserializeBenefitLimit(subscription.SubscriptionPlan.BenefitLimit);
            var planBenefit = planBenefitLimit.FirstOrDefault(b => b.FeatureCode == featureCode);

            return (benefit.Usage, planBenefit?.Usage);
        }

        public async Task ResetMonthlyBenefitsIfNeededAsync(long userId)
        {
            var subscription = await GetActiveSubscriptionAsync(userId);
            if (subscription == null) return;

            var currentDate = DateTime.UtcNow;
            var createdDate = subscription.CreatedDate;

            // Check if we're in a new month since subscription creation
            bool shouldReset = false;

            if (createdDate.Year < currentDate.Year)
            {
                shouldReset = true;
            }
            else if (createdDate.Year == currentDate.Year && createdDate.Month < currentDate.Month)
            {
                shouldReset = true;
            }

            if (!shouldReset) return;

            // Get benefit usage and plan limits
            var benefitUsage = DeserializeBenefitUsage(subscription.BenefitUsed);
            var planBenefitLimit = DeserializeBenefitLimit(subscription.SubscriptionPlan.BenefitLimit);

            // Reset all monthly benefits
            foreach (var benefit in benefitUsage.Where(b => b.ResetType == ResetType.Monthly))
            {
                var planBenefit = planBenefitLimit.FirstOrDefault(b => b.FeatureCode == benefit.FeatureCode);
                if (planBenefit != null)
                {
                    benefit.Usage = planBenefit.Usage; // Reset to plan limit
                }
            }

            // Save back to database
            subscription.BenefitUsed = JsonSerializer.Serialize(benefitUsage);
            _unitOfWork.UserSubscriptionRepository.UpdateAsync(subscription);
            await _unitOfWork.SaveAsync();
        }

        private async Task<Repository.Entities.UserSubscription?> GetActiveSubscriptionAsync(long userId)
        {
            var subscriptions = await _unitOfWork.UserSubscriptionRepository.GetAllAsync();
            return subscriptions
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.UserId == userId && s.IsActive && s.DateExp > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedDate)
                .FirstOrDefault();
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
