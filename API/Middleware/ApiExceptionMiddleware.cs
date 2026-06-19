using System.Net;
using System.Text.Json;

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
                logger.LogError(
                    exception,
                    "Unhandled API error for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var message = environment.IsDevelopment()
                    ? exception.Message
                    : "The API could not complete the request.";

                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    message
                }));
            }
        }
    }
}
