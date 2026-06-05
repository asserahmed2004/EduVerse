using API.Authorization;
using Application.DTOs.Admin;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.AdminAccess)]
    public class AdminController(IActivityLogService activityLogService, IAdminService adminService) : ControllerBase
    {
        [HttpGet("ActivityLogs")]
        public async Task<IActionResult> GetActivityLogs([FromQuery] ActivityLogQuery query)
        {
            var result = await activityLogService.GetLogsAsync(query);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GlobalSearch")]
        public async Task<IActionResult> GlobalSearch([FromQuery] string? q)
        {
            var result = await adminService.GlobalSearchAsync(q);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("UserDetails/{userId}")]
        public async Task<IActionResult> GetUserDetails(string userId)
        {
            var result = await adminService.GetUserDetailsAsync(userId);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
