using System;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.UserOccasionModels
{
    public class UserOccasionUpdateModel
    {
        /// <summary>
        /// Reference to master Occasion (optional)
        /// </summary>
        /// <example>5</example>
        public long? OccasionId { get; set; }

        /// <summary>
        /// Name/title of the occasion
        /// </summary>
        /// <example>Updated Client Meeting</example>
        [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// Detailed description of the occasion
        /// </summary>
        /// <example>Updated Q4 Business Review Meeting</example>
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Date of the occasion
        /// </summary>
        /// <example>2025-12-16T15:00:00</example>
        public DateTime? DateOccasion { get; set; }

        /// <summary>
        /// Start time (optional)
        /// </summary>
        /// <example>2025-12-16T15:00:00</example>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time (optional)
        /// </summary>
        /// <example>2025-12-16T17:00:00</example>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Weather information (optional)
        /// </summary>
        /// <example>Cloudy, 18°C</example>
        [MaxLength(255, ErrorMessage = "Weather snapshot cannot exceed 255 characters")]
        public string? WeatherSnapshot { get; set; }
    }
}
