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
    public class AssignmentProgressController(IUserService userService) : ControllerBase
    {
        [HttpGet("Course/{courseId}")]
        public async Task<IActionResult> GetCourseAssignmentProgress(Guid courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var progress = await userService.GetAssignmentProgress(courseId, userId);
            if (progress == null)
                return NotFound(new { success = false, message = "Assignment progress not found or student is not enrolled." });

            return Ok(new { success = true, message = "Assignment progress retrieved successfully", data = progress });
        }
    }
}
