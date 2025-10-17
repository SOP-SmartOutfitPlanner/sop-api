using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.EmailModels
{
    public class PasswordResetSuccessEmailTemplateModel
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string ResetTime { get; set; }
    }
}
