namespace SOPServer.API.Configurations
{
    public static class CorsConfiguration
    {
        public const string PolicyName = "app-cors";

        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .WithExposedHeaders("X-Pagination")
                        .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
