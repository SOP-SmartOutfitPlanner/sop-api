namespace SOPServer.API.Configurations
{
    public static class HttpClientConfiguration
    {
        public static IServiceCollection AddHttpClientConfiguration(this IServiceCollection services)
        {
            // General SOP HTTP Client
            services.AddHttpClient("SOPHttpClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Rembg Service Client
            services.AddHttpClient("RembgClient", client =>
            {
                client.BaseAddress = new Uri("https://rembg.wizlab.io.vn/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}
