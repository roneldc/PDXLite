using PDXLite.DTOs.Error;
using System.Net;
using System.Text.Json;

namespace PDXLite.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. Path: {Path}, Method: {Method}, IP: {IP}",
                    context.Request.Path,
                    context.Request.Method,
                    context.Connection.RemoteIpAddress);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                Path = context.Request.Path,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Error = new ErrorDetail
                    {
                        Code = ErrorCodes.UNAUTHORIZED,
                        Message = "You are not authorized to access this resource"
                    };
                    break;

                case ArgumentException argEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Error = new ErrorDetail
                    {
                        Code = ErrorCodes.INVALID_INPUT,
                        Message = argEx.Message
                    };
                    break;

                case InvalidOperationException invOpEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Error = new ErrorDetail
                    {
                        Code = ErrorCodes.CONFIGURATION_ERROR,
                        Message = invOpEx.Message
                    };
                    break;

                case HttpRequestException httpEx:
                    context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    errorResponse.Error = new ErrorDetail
                    {
                        Code = ErrorCodes.SERVICE_UNAVAILABLE,
                        Message = "External service is temporarily unavailable"
                    };
                    _logger.LogWarning(httpEx, "External service error");
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Error = new ErrorDetail
                    {
                        Code = ErrorCodes.INTERNAL_ERROR,
                        Message = _env.IsDevelopment()
                            ? exception.Message
                            : "An internal error occurred. Please try again later."
                    };
                    break;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _env.IsDevelopment()
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
        }
    }
}
