using SOPServer.Service.Services.Implements;

namespace SOPServer.API.Configurations
{
    public static class HostedServiceConfiguration
    {
        public static IServiceCollection AddHostedServiceConfiguration(this IServiceCollection services)
        {
            services.AddHostedService<PaymentPeriodicService>();
            services.AddHostedService<SubscriptionPeriodicService>();

            return services;
        }
    }
}
