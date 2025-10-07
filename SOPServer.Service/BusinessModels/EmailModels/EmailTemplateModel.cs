using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.EmailModels
{
    public class OtpEmailTemplateModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; }
    }

    public class WelcomeEmailTemplateModel
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
