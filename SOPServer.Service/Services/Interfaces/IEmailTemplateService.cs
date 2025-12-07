using SOPServer.Service.BusinessModels.EmailModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<string> GenerateOtpEmailAsync(OtpEmailTemplateModel model);
        Task<string> GenerateWelcomeEmailAsync(WelcomeEmailTemplateModel model);
        Task<string> GeneratePasswordResetSuccessEmailAsync(PasswordResetSuccessEmailTemplateModel model);
        Task<string> GeneratePasswordChangedEmailAsync(PasswordChangedEmailTemplateModel model);
    }
}
