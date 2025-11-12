using SOPServer.Repository.Enums;
using System;

namespace SOPServer.Service.BusinessModels.NotificationModels
{
    public class NotificationModel
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Href { get; set; }
        public NotificationType? Type { get; set; }
        public string? ImageUrl { get; set; }
        public long? ActorUserId { get; set; }
        public string? ActorDisplayName { get; set; }
        public string? ActorAvatarUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SystemNotificationModel
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Href { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationRequestModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Href { get; set; }
        public string? ImageUrl { get; set; }
        public long? ActorUserId { get; set; }
    }
}
