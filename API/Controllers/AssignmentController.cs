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
    public class AssignmentController(IUserService userService) : ControllerBase
    {
        [HttpPost("Submit/{assignmentId}")]
        [Authorize(Roles = AppRoles.StudentAccess)]
        public async Task<IActionResult> Submit(Guid assignmentId, [FromForm] SubmitAssignmentRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            request.AssignmentId = assignmentId;
            var result = await userService.SubmitAssignment(request, userId);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
