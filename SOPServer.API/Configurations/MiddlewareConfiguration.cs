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

            // 1. Authentication - X�c th?c JWT token
            app.UseAuthentication();

            // 2. Ki?m tra token c� t?n t?i trong Redis kh�ng
            app.UseMiddleware<AuthenHandlingMiddleware>();

            // 3. Authorization - Ph�n quy?n d?a tr�n roles
            app.UseAuthorization();

            // 4. Exception handling - B?t v� x? l� t?t c? exceptions
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            return app;
        }
    }
}
