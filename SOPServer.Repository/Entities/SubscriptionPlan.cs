using System;
using System.Collections.Generic;
using SOPServer.Repository.Enums;

namespace SOPServer.Repository.Entities
{
    public partial class SubscriptionPlan : BaseEntity
    {
        public long Price { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public string? BenefitLimit { get; set; }

        public SubscriptionPlanStatus Status { get; set; }

        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}