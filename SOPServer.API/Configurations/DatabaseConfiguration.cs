using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;

namespace SOPServer.API.Configurations
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<SOPServerContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("SOPServerLocal");
                options.UseSqlServer(connectionString);
            });

            return services;
        }
    }
}
