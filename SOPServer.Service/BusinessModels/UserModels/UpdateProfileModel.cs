using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.UserModels
{
    public class UpdateProfileModel
    {
        [MaxLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        public string? DisplayName { get; set; }

        public DateOnly? Dob { get; set; }

        public Gender? Gender { get; set; }

        public List<string>? PreferedColor { get; set; }

        public List<string>? AvoidedColor { get; set; }

        [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }

        [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        /// <summary>
        /// Avatar URL. Can be updated directly or use the dedicated avatar upload endpoint
        /// </summary>
        [MaxLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
        public string? AvtUrl { get; set; }

        /// <summary>
        /// Full body image URL for virtual try-on feature
        /// </summary>
        [MaxLength(500, ErrorMessage = "Try-on image URL cannot exceed 500 characters")]
        public string? TryOnImageUrl { get; set; }

        public long? JobId { get; set; }

        /// <summary>
        /// Custom job name. If provided, a new job will be created and used instead of JobId
        /// </summary>
        [MaxLength(100, ErrorMessage = "Other job cannot exceed 100 characters")]
        public string? OtherJob { get; set; }

        public List<long>? StyleIds { get; set; }

        /// <summary>
        /// Custom style names. New styles will be created for each name provided
        /// </summary>
        public List<string>? OtherStyles { get; set; }
    }
}
