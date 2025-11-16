using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.NotificationModels
{
    /// <summary>
    /// Model for deleting multiple notifications by IDs
    /// </summary>
    public class DeleteNotificationsModel
    {
        /// <summary>
        /// List of UserNotification IDs to delete
        /// </summary>
        /// <example>[1, 2, 3, 4, 5]</example>
        [Required(ErrorMessage = "NotificationIds is required")]
        [MinLength(1, ErrorMessage = "At least one notification ID is required")]
        public List<long> NotificationIds { get; set; } = new List<long>();
    }
}
