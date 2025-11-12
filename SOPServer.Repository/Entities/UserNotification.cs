using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    public partial class UserNotification : BaseEntity
    {
        public long NotificationId { get; set; }
        public long UserId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        // Aggregation support
        public string? GroupKey { get; set; }  // For grouping similar notifications
        public int? AggregationCount { get; set; }  // "5 people liked your post"

        public virtual Notification Notification { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
