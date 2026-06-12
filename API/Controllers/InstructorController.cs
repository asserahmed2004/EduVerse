using API.Authorization;
using Application.DTOs.Learning;
using Application.Services.Implementitions;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.InstructorAccess)]
    public class InstructorController(IInstructorService instructorService) : ControllerBase
    {
        [HttpGet("Overview")]
        public async Task<IActionResult> Overview()
        {
            var result = await instructorService.GetOverviewAsync(CurrentUserId());
            return result.success ? Ok(result) : BadRequest(result);
        }
        [HttpPost("Mark")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> Mark(Guid sessionId, string userId)
        {



            var result = await instructorService.MarkAttendance(sessionId, userId);
            return result.success ? Ok(result) : BadRequest(result);
        }



        [HttpGet("Sessions")]
        public async Task<IActionResult> Sessions()
        {
            var result = await instructorService.GetSessionsAsync(CurrentUserId());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Students")]
        public async Task<IActionResult> Students()
        {
            var result = await instructorService.GetStudentsAsync(CurrentUserId());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Submissions")]
        public async Task<IActionResult> Submissions()
        {
            var result = await instructorService.GetSubmissionsAsync(CurrentUserId());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Submission/{assignmentId}/{studentId}")]
        public async Task<IActionResult> Submission(Guid assignmentId, string studentId)
        {
            var result = await instructorService.GetSubmissionAsync(assignmentId, studentId, CurrentUserId());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("GradeSubmission/{assignmentId}/{studentId}")]
        public async Task<IActionResult> GradeSubmission(Guid assignmentId, string studentId, [FromBody] GradeSubmissionRequest request)
        {
            var result = await instructorService.GradeSubmissionAsync(assignmentId, studentId, request, CurrentUserId());
            return result.success ? Ok(result) : BadRequest(result);
        }

        private string CurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
    }
}
