using System;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.OutfitCalendarModels
{
    public class OutfitCalendarCreateModel
    {
        /// <summary>
        /// ID of the outfit to assign to calendar
        /// </summary>
        /// <example>10</example>
        [Required(ErrorMessage = "Outfit ID is required")]
        public long OutfitId { get; set; }

        /// <summary>
        /// ID of the user occasion (optional - if linking to an event)
        /// </summary>
        /// <example>5</example>
        public long? UserOccasionId { get; set; }

        /// <summary>
        /// Date when the outfit will be used
        /// </summary>
        /// <example>2025-12-15T14:00:00</example>
        [Required(ErrorMessage = "Date is required")]
        public DateTime DateUsed { get; set; }
    }
}
