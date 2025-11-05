using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.UserModels
{
    public class UserPublicModel
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string? AvtUrl { get; set; }
        public DateOnly? Dob { get; set; }
        public Gender Gender { get; set; }
        public List<string>? PreferedColor { get; set; }
        public List<string>? AvoidedColor { get; set; }
        public string? Location { get; set; }
        public string? Bio { get; set; }

        public long? JobId { get; set; }
        public string? JobName { get; set; }
        public string? JobDescription { get; set; }

        public List<UserStyleModel> UserStyles { get; set; } = new List<UserStyleModel>();
    }
}
