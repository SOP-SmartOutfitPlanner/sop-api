using SOPServer.API.Middlewares;

namespace SOPServer.API.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static IApplicationBuilder UseMiddlewareConfiguration(
            this IApplicationBuilder app,
            string corsPolicyName)
        {
            app.UseHttpsRedirection();

            app.UseCors(corsPolicyName);

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseAuthentication();

            app.UseMiddleware<AuthenHandlingMiddleware>();

            app.UseAuthorization();

            return app;
        }
    }
}
