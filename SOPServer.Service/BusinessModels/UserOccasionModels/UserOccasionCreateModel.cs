using System;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.UserOccasionModels
{
    public class UserOccasionCreateModel
    {
        /// <summary>
        /// Reference to master Occasion (optional)
        /// </summary>
        /// <example>5</example>
        public long? OccasionId { get; set; }

        /// <summary>
        /// Name/title of the occasion
        /// </summary>
        /// <example>Client Meeting with ABC Corp</example>
        [Required(ErrorMessage = "Occasion name is required")]
        [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; }

        /// <summary>
        /// Detailed description of the occasion
        /// </summary>
        /// <example>Q4 Business Review Meeting</example>
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Date of the occasion
        /// </summary>
        /// <example>2025-12-15T14:00:00</example>
        [Required(ErrorMessage = "Date is required")]
        public DateTime DateOccasion { get; set; }

        /// <summary>
        /// Start time (optional)
        /// </summary>
        /// <example>2025-12-15T14:00:00</example>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time (optional)
        /// </summary>
        /// <example>2025-12-15T16:00:00</example>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Weather information (optional, can be auto-filled)
        /// </summary>
        /// <example>Sunny, 22°C</example>
        [MaxLength(255, ErrorMessage = "Weather snapshot cannot exceed 255 characters")]
        public string? WeatherSnapshot { get; set; }
    }
}
