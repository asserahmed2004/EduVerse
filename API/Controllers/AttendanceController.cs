using API.Authorization;
using Application.DTOs.Learning;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AttendanceController(IUserService userService, IInstructorService instructorService) : ControllerBase
    {
        [HttpPost("CreateSessionQr/{sessionId}")]
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
        public async Task<IActionResult> CreateSessionQr(Guid sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var result = await instructorService.CreateSessionQrAsync(sessionId, userId, User.HasRole(AppRoles.Admin) || User.HasRole(AppRoles.OrganizationAdmin));
            return result.success ? Ok(result) : BadRequest(result);
        }

        
        [HttpGet("Session/{sessionId}")]
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
        public async Task<IActionResult> Session(Guid sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var result = await instructorService.GetSessionAttendanceAsync(sessionId, userId, User.HasRole(AppRoles.Admin) || User.HasRole(AppRoles.OrganizationAdmin));
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("Mark/{sessionId}")]
        [Authorize(Roles = AppRoles.StudentAccess)]
        public async Task<IActionResult> Mark(Guid sessionId, [FromBody] MarkAttendanceRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var result = await userService.MarkAttendance(sessionId, userId, request?.AttendanceCode ?? string.Empty);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
