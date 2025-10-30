using SOPServer.Service.SettingModels;
using StackExchange.Redis;

namespace SOPServer.API.Configurations
{
    public static class RedisConfiguration
    {
        public static IServiceCollection AddRedisConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<RedisSettings>(
                configuration.GetSection("RedisSettings"));

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConnectionString = configuration["RedisSettings:RedisConnectionString"];
                var configOptions = ConfigurationOptions.Parse(redisConnectionString, true);
                return ConnectionMultiplexer.Connect(configOptions);
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["RedisSettings:RedisConnectionString"];
                options.InstanceName = configuration["RedisSettings:InstanceName"];
            });

            return services;
        }

        public static async Task<IApplicationBuilder> UseRedisHealthCheckAsync(this IApplicationBuilder app)
        {
            try
            {
                var redis = app.ApplicationServices.GetRequiredService<IConnectionMultiplexer>();
                var db = redis.GetDatabase();
                await db.PingAsync();
                Console.WriteLine("Redis connection successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis connection failed: {ex.Message}");
            }

            return app;
        }
    }
}
