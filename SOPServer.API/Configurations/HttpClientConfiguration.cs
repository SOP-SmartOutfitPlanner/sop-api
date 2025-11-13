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

            services.AddHttpClient("AnalysisClient", client =>
            {
                client.BaseAddress = new Uri("https://storage.wizlab.io.vn/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddHttpClient("SplitItem", client =>
            {
                client.BaseAddress = new Uri("https://split-item.wizlab.io.vn/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            return services;
        }
    }
}
