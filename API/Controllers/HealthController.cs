using InfraStructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace API.Controllers
{
    [Route("health")]
    [ApiController]
    [AllowAnonymous]
    public class HealthController(AppDbContext dbContext) : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                success = true,
                message = "EduVerse API is running",
                utcNow = DateTime.UtcNow
            });
        }

        [HttpGet("db")]
        public async Task<IActionResult> Database(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                stopwatch.Stop();

                return Ok(new
                {
                    success = canConnect,
                    canConnect,
                    elapsedMs = stopwatch.ElapsedMilliseconds,
                    error = canConnect ? null : "SQL Server connection could not be established."
                });
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    canConnect = false,
                    elapsedMs = stopwatch.ElapsedMilliseconds,
                    error = exception.Message
                });
            }
        }
    }
}
