using Microsoft.Extensions.Options;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;

namespace SOPServer.API.Configurations
{
    public static class QDrantConfiguration
    {
        public static async Task<IApplicationBuilder> UseQDrantInitializationAsync(this IApplicationBuilder app)
        {
            try
            {
                using var scope = app.ApplicationServices.CreateScope();
                var qdrantService = scope.ServiceProvider.GetRequiredService<IQdrantService>();

                await qdrantService.EnsureCollectionExistsAsync();

                Console.WriteLine("Qdrant collection initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Qdrant collection initialization failed: {ex.Message}");
            }

            return app;
        }
    }
}
