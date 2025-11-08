using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities
{
    public partial class SubscriptionPlan : BaseEntity
    {
        public long Price { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public int ChatRateLimit { get; set; }

        public int ItemLimit { get; set; }

        public int AISuggestionLimit { get; set; }

        public bool IsSuggestWeather { get; set; }

        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}