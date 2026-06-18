using System.Diagnostics;

namespace API.Middleware
{
    public class RequestPerformanceMiddleware(
        RequestDelegate next,
        ILogger<RequestPerformanceMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await next(context);
                stopwatch.Stop();
                logger.LogInformation(
                    "{Method} {Path} => {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                logger.LogError(
                    exception,
                    "{Method} {Path} => 500 in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
