using System.Net;
using System.Text.Json;
using Application.Exceptions;

namespace API.Middleware
{
    public class ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                if (exception is PaymobException)
                {
                    logger.LogWarning(
                        exception,
                        "PayMob request failed for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                }
                else
                {
                    logger.LogError(
                        exception,
                        "Unhandled API error for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                }

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = exception is PaymobException paymobException
                    ? paymobException.StatusCode
                    : (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var message = exception switch
                {
                    PaymobException paymobError when environment.IsDevelopment() => paymobError.Message,
                    PaymobException paymobError => paymobError.ClientMessage,
                    _ when environment.IsDevelopment() => exception.Message,
                    _ => "The API could not complete the request."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    message
                }));
            }
        }
    }
}
