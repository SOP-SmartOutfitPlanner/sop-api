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
        /// <example>["pink", "bright pink", "pastel pink"]</example>
         public List<string>? PreferedColor { get; set; }
        /// <example>["black", "dark black", "white"]</example>
        public List<string>? AvoidedColor { get; set; }
        /// <example>0</example>
        public Gender Gender { get; set; }
        /// <example>Nha Trang</example>
        public string? Location { get; set; }
        /// <example>1</example>
        public long? JobId { get; set; }
        /// <example>Software Engineer</example>
        public string? OtherJob { get; set; }
        /// <example>2000-12-31</example>
        public DateOnly? Dob { get; set; }
        /// <example>This is my bio</example>
        public string? Bio { get; set; }
        /// <example>[1, 2]</example>
        public List<long>? StyleIds { get; set; }
        /// <example>["Minimalist", "Bohemian"]</example>
        public List<string>? OtherStyles { get; set; }

    }
}

