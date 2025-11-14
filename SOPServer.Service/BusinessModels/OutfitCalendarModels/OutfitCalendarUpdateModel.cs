using System;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.OutfitCalendarModels
{
    public class OutfitCalendarUpdateModel
    {
        /// <summary>
        /// ID of the outfit to assign to calendar
        /// </summary>
        /// <example>10</example>
        public long? OutfitId { get; set; }

        /// <summary>
        /// ID of the user occasion (required when IsDaily is false)
        /// Should NOT be provided when IsDaily is true
        /// </summary>
        /// <example>5</example>
        public long? UserOccasionId { get; set; }

        /// <summary>
        /// Indicates if this is a daily outfit (no specific occasion)
        /// When true, automatically creates/links to a "Daily" user occasion
        /// When false, UserOccasionId is required
        /// </summary>
        /// <example>true</example>
        public bool? IsDaily { get; set; }

        /// <summary>
        /// Start time when the outfit will be worn (required when IsDaily is true)
        /// Should NOT be provided when IsDaily is false
        /// </summary>
        /// <example>2025-01-15T08:00:00</example>
        public DateTime? Time { get; set; }

        /// <summary>
        /// End time for the outfit (optional, used when IsDaily is true)
        /// </summary>
        /// <example>2025-01-15T17:00:00</example>
        public DateTime? EndTime { get; set; }
    }
}
