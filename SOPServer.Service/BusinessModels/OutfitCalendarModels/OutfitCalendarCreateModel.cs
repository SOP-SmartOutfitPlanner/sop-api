using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.OutfitCalendarModels
{
    public class OutfitCalendarCreateModel
    {
        /// <summary>
        /// IDs of the outfits to assign to calendar (can be multiple)
        /// </summary>
        /// <example>[10, 15, 20]</example>
        [Required(ErrorMessage = "At least one outfit ID is required")]
        [MinLength(1, ErrorMessage = "At least one outfit ID is required")]
        public List<long> OutfitIds { get; set; } = new List<long>();

        /// <summary>
        /// ID of the user occasion (optional - if linking to an event)
        /// Note: If IsDaily is true, this will be auto-set to the user's "Daily" occasion
        /// </summary>
        /// <example>5</example>
        public long? UserOccasionId { get; set; }

        /// <summary>
        /// Indicates if this is a daily outfit (no specific occasion)
        /// When true, automatically creates/links to a "Daily" user occasion
        /// </summary>
        /// <example>true</example>
        public bool IsDaily { get; set; }

        /// <summary>
        /// Time when the outfit will be worn (required when IsDaily is true)
        /// Used to set the StartTime on the Daily UserOccasion
        /// </summary>
        /// <example>08:00:00</example>
        public TimeSpan? Time { get; set; }

        /// <summary>
        /// End time for the outfit (optional, used when IsDaily is true)
        /// Used to set the EndTime on the Daily UserOccasion
        /// </summary>
        /// <example>17:00:00</example>
        public TimeSpan? EndTime { get; set; }
    }
}
