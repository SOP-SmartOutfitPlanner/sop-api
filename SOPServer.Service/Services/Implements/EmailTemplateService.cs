using Microsoft.AspNetCore.Hosting;
using SOPServer.Service.BusinessModels.EmailModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IWebHostEnvironment _environment;

        public EmailTemplateService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> GenerateOtpEmailAsync(OtpEmailTemplateModel model)
        {
            var template = await LoadTemplateAsync("OtpEmail.html");

            var body = template
                .Replace("{{DisplayName}}", model.DisplayName)
                .Replace("{{Otp}}", model.Otp)
                .Replace("{{ExpiryMinutes}}", model.ExpiryMinutes.ToString());

            return body;
        }

        public async Task<string> GenerateWelcomeEmailAsync(WelcomeEmailTemplateModel model)
        {
            var template = await LoadTemplateAsync("WelcomeEmail.html");

            var body = template.Replace("{{DisplayName}}", model.DisplayName);

            return body;
        }

        public async Task<string> GeneratePasswordResetSuccessEmailAsync(PasswordResetSuccessEmailTemplateModel model)
        {
            var template = await LoadTemplateAsync("PasswordResetSuccessEmail.html");

            var body = template
                .Replace("{{DisplayName}}", model.DisplayName)
                .Replace("{{Email}}", model.Email)
                .Replace("{{ResetTime}}", model.ResetTime);

            return body;
        }

        public async Task<string> GeneratePasswordChangedEmailAsync(PasswordChangedEmailTemplateModel model)
        {
            var template = await LoadTemplateAsync("PasswordChangedEmail.html");

            var body = template
                .Replace("{{DisplayName}}", model.DisplayName)
                .Replace("{{Email}}", model.Email)
                .Replace("{{ChangedTime}}", model.ChangedTime);

            return body;
        }

        public async Task<string> GenerateOtpPasswordChangeEmailAsync(OtpPasswordChangeEmailTemplateModel model)
        {
            var template = await LoadTemplateAsync("OtpPasswordChangeEmail.html");

            var body = template
                .Replace("{{DisplayName}}", model.DisplayName)
                .Replace("{{Otp}}", model.Otp)
                .Replace("{{ExpiryMinutes}}", model.ExpiryMinutes.ToString());

            return body;
        }

        private async Task<string> LoadTemplateAsync(string templateName)
        {
            try
            {
                var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "Emails", templateName);

                if (File.Exists(templatePath))
                {
                    return await File.ReadAllTextAsync(templatePath);
                }

                var assembly = typeof(EmailTemplateService).Assembly;
                var resourceName = $"SOPServer.Service.Templates.Emails.{templateName}";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    var names = string.Join(", ", assembly.GetManifestResourceNames());
                    throw new FileNotFoundException(
                        $"Email template '{templateName}' not found at '{templatePath}'. " +
                        $"Embedded resources available: {names}");
                }
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load email template '{templateName}': {ex.Message}", ex);
            }
        }
    }
}
