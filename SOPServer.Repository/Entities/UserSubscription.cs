using System;

namespace SOPServer.Repository.Entities
{
    public partial class UserSubscription : BaseEntity
    {
        public long UserId { get; set; }

        public long SubscriptionPlanId { get; set; }

        public DateTime DateExp { get; set; }

        public bool IsActive { get; set; }

        public string? BenefitUsed { get; set; }

        public virtual User User { get; set; }

        public virtual SubscriptionPlan SubscriptionPlan { get; set; }
        public virtual ICollection<UserSubscriptionTransaction> UserSubscriptionTransactions { get; set; } = new List<UserSubscriptionTransaction>();
    }
}