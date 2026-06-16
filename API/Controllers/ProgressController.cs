using API.Authorization;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.StudentAccess)]
    public class ProgressController(IUserService userService) : ControllerBase
    {
        [HttpPost("ToggleSessionDone/{sessionId}")]
        public async Task<IActionResult> ToggleSessionDone(Guid sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var result = await userService.ToggleSessionDone(sessionId, userId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Course/{courseId}")]
        public async Task<IActionResult> GetCourseProgress(Guid courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var progress = await userService.GetCourseProgress(courseId, userId);
            if (progress == null)
                return NotFound(new { success = false, message = "Course progress not found or student is not enrolled." });

            return Ok(new { success = true, message = "Personal course progress retrieved successfully", data = progress });
        }
    }
}
