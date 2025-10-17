using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.AuthenModels
{
    public class VerifyResetOtpRequestModel
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
}
