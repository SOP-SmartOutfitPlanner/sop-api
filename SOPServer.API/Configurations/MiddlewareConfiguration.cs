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

            // 1. Authentication - Xác th?c JWT token
            app.UseAuthentication();

            // 2. Ki?m tra token có t?n t?i trong Redis không
            app.UseMiddleware<AuthenHandlingMiddleware>();

            // 3. Authorization - Phân quy?n d?a trên roles
            app.UseAuthorization();

            // 4. Exception handling - B?t và x? lý t?t c? exceptions
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            return app;
        }
    }
}
