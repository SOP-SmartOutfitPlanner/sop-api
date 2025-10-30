using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Text.Json.Serialization;

namespace SOPServer.API.Configurations
{
    public static class ApiConfiguration
    {
        public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
        {
            // Add Controllers with JSON options
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            });

            // Configure API Behavior for Model Validation
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    var response = new BaseResponseModel
                    {
                        StatusCode = 400,
                        Message = string.Join("; ", errors)
                    };

                    return new BadRequestObjectResult(response);
                };
            });

            services.AddEndpointsApiExplorer();
            services.AddHttpContextAccessor();

            return services;
        }
    }
}
