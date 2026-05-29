using API.Authorization;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DashboardController(IDashboardService dashboardService) : ControllerBase
    {
        [HttpGet("OrganizationStats")]
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
        public async Task<IActionResult> GetOrganizationStats()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { success = false, message = "You must be logged in" });
            }

            if (!User.IsInRole(AppRoles.Admin) &&
                !User.IsInRole(AppRoles.OrganizationAdmin) &&
                !User.IsInRole(AppRoles.Instructor))
            {
                return Forbid();
            }

            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await dashboardService.GetOrganizationStatsAsync(
                    currentUserId,
                    User.IsInRole(AppRoles.Admin),
                    User.IsInRole(AppRoles.OrganizationAdmin),
                    User.IsInRole(AppRoles.Instructor));

                if (!result.success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Unexpected error while retrieving organization stats"
                });
            }
        }
    }
}
