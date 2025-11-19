namespace SOPServer.API.Configurations
{
    public static class SignalRConfiguration
    {
        public static IServiceCollection AddSignalRConfiguration(this IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
            });
            return services;
        }
    }
}
