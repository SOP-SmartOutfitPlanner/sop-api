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

        public UserSubscriptionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

            // Deserialize plan benefits to initialize user benefits
            var planBenefits = DeserializeBenefitLimit(plan.BenefitLimit);

            // Create user subscription
            var userSubscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                DateExp = DateTime.UtcNow.AddMonths(1), // 1 month subscription
                IsActive = true,
                BenefitUsed = JsonSerializer.Serialize(planBenefits), // Initialize with full credits
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.UserSubscriptionRepository.AddAsync(userSubscription);
            await _unitOfWork.SaveAsync();

            // Reload with navigation properties
            var createdSubscription = await _unitOfWork.UserSubscriptionRepository.GetQueryable()
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.Id == userSubscription.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Subscription purchased successfully",
                Data = _mapper.Map<UserSubscriptionModel>(createdSubscription)
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
