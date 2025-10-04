using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IOtpService
    {
        Task<BaseResponseModel> SendOtpAsync(string email);
        Task<BaseResponseModel> VerifyOtpAsync(string email, string otp);
        Task<bool> IsEmailVerifiedAsync(string email);
    }
}
