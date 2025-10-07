using Microsoft.AspNetCore.Hosting;
using SOPServer.Service.BusinessModels.EmailModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        private async Task<string> LoadTemplateAsync(string templateName)
        {
            try
            {
                // Calculate the path to SOPServer.Service project (one level up from API)
                var serviceProjectPath = Path.GetFullPath(
                    Path.Combine(_environment.ContentRootPath, "..", "SOPServer.Service")
                );
                var templatePath = Path.Combine(serviceProjectPath, "Templates", "Emails", templateName);

                if (File.Exists(templatePath))
                {
                    return await File.ReadAllTextAsync(templatePath);
                }

                // Fallback to embedded resources if file not found
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"SOPServer.Service.Templates.Emails.{templateName}";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new NotFoundException($"Email template '{templateName}' not found at '{templatePath}'");
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
