using SOPServer.Repository.Enums;
using System;

namespace SOPServer.Repository.Entities
{
    public partial class UserSubscriptionTransaction : BaseEntity
    {
        public long UserSubscriptionId { get; set; }
        public int TransactionCode { get; set; }
        public decimal Price { get; set; }

        public TransactionStatus Status { get; set; }

        public string? Description { get; set; }
        public virtual UserSubscription UserSubscription { get; set; }
    }
}
