using SOPServer.Service.BusinessModels.SubscriptionLimitModels;
using SOPServer.Service.BusinessModels.SubscriptionPlanModels;

namespace SOPServer.Service.BusinessModels.UserSubscriptionModels
{
    public class UserSubscriptionModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long SubscriptionPlanId { get; set; }
        public DateTime DateExp { get; set; }
        public bool IsActive { get; set; }
        public List<Benefit> BenefitUsage { get; set; } = new List<Benefit>();
        public SubscriptionPlanModel? SubscriptionPlan { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
