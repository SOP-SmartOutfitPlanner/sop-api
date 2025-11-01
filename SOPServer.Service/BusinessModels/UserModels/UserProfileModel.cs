using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.UserModels
{
    public class UserProfileModel
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
        public bool IsVerifiedEmail { get; set; }
        public bool IsStylist { get; set; }
        public bool IsPremium { get; set; }
        public bool IsLoginWithGoogle { get; set; }
        public bool? IsFirstTime { get; set; }
        public Role Role { get; set; }
       
        public long? JobId { get; set; }
        public string? JobName { get; set; }
        public string? JobDescription { get; set; }
        
        public List<UserStyleModel> UserStyles { get; set; } = new List<UserStyleModel>();
    }

    public class UserStyleModel
    {
        public long Id { get; set; }
        public long StyleId { get; set; }
        public string StyleName { get; set; }
        public string StyleDescription { get; set; }
    }
}
