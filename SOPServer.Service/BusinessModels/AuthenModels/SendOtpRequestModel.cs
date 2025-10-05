using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.AuthenModels
{
    public class SendOtpRequestModel
    {
        public string Email { get; set; }
    }

    public class VerifyOtpRequestModel
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    public class RegisterWithOtpModel
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Otp { get; set; }
    }
}
