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

        public long? JobId { get; set; }

        public List<long>? StyleIds { get; set; }
    }
}
