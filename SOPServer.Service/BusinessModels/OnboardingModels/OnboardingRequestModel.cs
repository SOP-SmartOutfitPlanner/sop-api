using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.OnboardingModels
{
    public class OnboardingRequestModel
    {
        public string? PreferedColor { get; set; }
        public string? AvoidedColor { get; set; }
        public Gender Gender { get; set; }
        public string? Location { get; set; }
        public long? JobId { get; set; }
        public DateOnly? Dob { get; set; }
        public string? Bio { get; set; }
    }
}

