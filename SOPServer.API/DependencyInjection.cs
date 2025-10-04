using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfractstructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IGeminiService, GeminiService>();
            services.AddScoped<IItemService, ItemService>();

            services.AddScoped<IRedisService, RedisService>();
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
