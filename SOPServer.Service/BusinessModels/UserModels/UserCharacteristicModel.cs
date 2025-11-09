using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.UserModels
{
    public class UserCharacteristicModel
    {
        public string DisplayName { get; set; }

        public DateOnly? Dob { get; set; }

        public Gender Gender { get; set; }

        public List<string>? PreferedColor { get; set; }

        public List<string>? AvoidedColor { get; set; }

        public string? Location { get; set; }

        public string? Bio { get; set; }

        public string? Job { get; set; }

        public List<string>? Styles { get; set; }
    }
}
