using SOPServer.Service.Mappers;
using SOPServer.Service.SettingModels;

namespace SOPServer.API.Configurations
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddServiceConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add AutoMapper
            services.AddAutoMapper(typeof(Program));
            services.AddAutoMapper(typeof(MapperConfigProfile).Assembly);

            // Configure Settings
            services.Configure<MailSettings>(
                configuration.GetSection("MailSettings"));

            services.Configure<GeminiSettings>(
                configuration.GetSection("Gemini"));

            services.Configure<FirebaseStorageSettings>(
                configuration.GetSection("FirebaseStorage"));

            services.Configure<MinioSettings>(
                configuration.GetSection("MinioStorage"));

            services.Configure<QDrantClientSettings>(
                configuration.GetSection("QDrantSettings"));

            services.Configure<PayOSSettings>(
                configuration.GetSection("PayOSSettings"));
            // Add Infrastructure Services
            services.AddInfractstructure(configuration);

            return services;
        }
    }
}
