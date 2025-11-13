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
        /// ID of the user occasion (optional - if linking to an event)
        /// </summary>
        /// <example>5</example>
        public long? UserOccasionId { get; set; }

        /// <summary>
        /// Indicates if this is a daily outfit (no specific occasion)
        /// </summary>
        /// <example>true</example>
        public bool? IsDaily { get; set; }

        /// <summary>
        /// Time when the outfit will be worn (used when IsDaily is true)
        /// </summary>
        /// <example>08:00:00</example>
        public TimeSpan? Time { get; set; }

        /// <summary>
        /// End time for the outfit (optional, used when IsDaily is true)
        /// </summary>
        /// <example>17:00:00</example>
        public TimeSpan? EndTime { get; set; }
    }
}
