using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    public partial class Notification : BaseEntity
    {
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? Href { get; set; }
        public NotificationType? Type { get; set; }
        public string? ImageUrl { get; set; }  // Icon/avatar for notification
        public string? Data { get; set; }  // JSON payload for complex data
        public long? ActorUserId { get; set; }  // Who triggered the notification
        public virtual User? ActorUser { get; set; }
        public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
    }
}
