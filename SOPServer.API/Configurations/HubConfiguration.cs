using Microsoft.AspNetCore.Routing;
using SOPServer.Service.Hubs;

namespace SOPServer.API.Configurations
{
    public static class HubConfiguration
    {
        public static IEndpointRouteBuilder UseHub(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<PaymentHub>("/paymentHub");
            return endpoints;
        }
    }
}
