namespace SOPServer.API.Configurations
{
    public static class ConfigurationSetup
    {
        public static IConfigurationBuilder AddConfigurationSetup(
            this IConfigurationBuilder configurationBuilder,
            IWebHostEnvironment environment)
        {
            if (!environment.IsDevelopment())
            {
                var deploymentPath = Environment.GetEnvironmentVariable("PATH_SOP");

                if (string.IsNullOrEmpty(deploymentPath))
                {
                    throw new InvalidOperationException(
                        "Environment variable PATH_SOP is not set for non-development environment.");
                }

                configurationBuilder.AddJsonFile(deploymentPath, optional: false, reloadOnChange: true);
            }

            return configurationBuilder;
        }
    }
}
