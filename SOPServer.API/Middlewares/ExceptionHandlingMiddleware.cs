using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;

namespace SOPServer.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BaseErrorResponseException ex)
            {
                await HandleAppExceptionAsync(ex, context);
            }
            catch (Exception ex)
            {
                //await HandleAppExceptionAsync(new BaseErrorResponseException(MessageConstants.INTERNAL_SERVER_ERROR, 500), context);
                await HandleAppExceptionAsync(new BaseErrorResponseException(ex.Message, 500), context);
            }
        }

        private async Task HandleAppExceptionAsync(BaseErrorResponseException ex, HttpContext context)
        {
            _logger.LogError(ex, "Handled exception");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex.HttpStatusCode;

            var response = new BaseResponseModel
            {
                StatusCode = ex.HttpStatusCode,
                Message = ex.Message,
                Data = ex.Data
            };

            var jsonResponse = JsonConvert.SerializeObject(response, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
