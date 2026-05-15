using Application.DTOs.Assignment;
using Application.DTOs.Course;
using Application.DTOs.Rating;
using Application.DTOs.Responses;
using Application.DTOs.Sessions;
using Application.Services.Interfaces;
using API.Authorization;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CourseController(ICourseService courseService,IMapper mapper) : ControllerBase
    {
        [HttpPost("Create")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> CreateCourse([FromForm] CreateCourse Course)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "User id claim is missing" });

            var result = await courseService.CreateCourse(Course, userId);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpDelete("Delete/{id}")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            if (!await CanManageCourse(id))
                return Forbid();

            var result = await courseService.DeleteCourse(id);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllCourses()
        {
            string userId;
            if (User.Identity.IsAuthenticated)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            else
            {
                userId = null;
            }

            var result = await courseService.GetAllCourses(userId);
            return Ok(result);
        }
        [HttpGet("GetByCategory/{category}")]
        public async Task<IActionResult> GetCoursesByCategory(Guid category)
        {
            string userId;
            if (User.Identity.IsAuthenticated)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            else
            {
                userId = null;
            }
            var result = await courseService.GetCourseByCategory(category, userId);
            return Ok(result);
        }
        [HttpGet("search/{query}")]
        public async Task<IActionResult> SearchCourses(string query)
        {
            string userId;
            if (User.Identity.IsAuthenticated)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            else
            {
                userId = null;
            }
            var result = await courseService.Search(query, userId);
            return Ok(result);
        }
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetCourseById(Guid id)
        {
            string userId;
            if (User.Identity.IsAuthenticated)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            else
            {
                userId = null;
            }
            var result = await courseService.GetCourseById(id,userId);
            if (result == null)
                return NotFound(new { success = false, message = "Course not found" });
            return Ok(result);
        }
        [HttpGet("GetByName/{name}")]
        public async Task<IActionResult> GetCourseByName(string name)
        {
            string userId;
            if (User.Identity.IsAuthenticated)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            else
            {
                userId = null;
            }
            var result = await courseService.GetCourseByName(name,userId);
            if (result == null)
                return NotFound(new { success = false, message = "Course not found" });
            return Ok(result);
        }
        [HttpPut("Update")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> UpdateCourse([FromForm] UpdateCourse Course)
        {
            if (!await CanManageCourse(Course.Id))
                return Forbid();

            var result = await courseService.UpdateCourse(Course);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpPost("AddRating")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> AddRating( CreateRating rating)
        {
            string userId;
            if (User.Identity.IsAuthenticated)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            else
            {
                return Unauthorized(new { success = false, message = "You must be logged in to add a rating" });
            }
            var result = await courseService.AddRating(rating, userId);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpPost("AddSession")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> AddSession([FromForm]CreateSessionRequest session)
        {
            var sessionEntity = mapper.Map<CreateSession>(session);
            sessionEntity.CourseId = Guid.Parse(session.Course);

            if (!await CanManageCourse(sessionEntity.CourseId))
                return Forbid();

            var result = await courseService.AddSession(sessionEntity);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("GetAllSessions/{courseId}")]
        public async Task<IActionResult> GetCourseAllSessions(Guid courseId)
        {
            var result = await courseService.GetCourseAllSessions(courseId);
            
            return Ok(result);
        }
        [HttpGet("GetSessionById/{id}")]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            var result = await courseService.GetSessionById(id);
            if (result == null)
                return NotFound(new { success = false, message = "Session not found" });
            return Ok(result);
        }
        [HttpGet("GetSessionByNumber/{courseId}/{sessionNumber}")]
        public async Task<IActionResult> GetSessionByNumber(Guid courseId, int sessionNumber)
        {
            var result = await courseService.GetSessionByNumber(courseId, sessionNumber);
            if (result == null)
                return NotFound(new { success = false, message = "Session not found" });
            return Ok(result);
        }
        [HttpPut("UpdateSession")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> UpdateSession([FromForm] UpdateSession session)
        {
            if (!await CanManageSession(session.Id))
                return Forbid();

            var result = await courseService.UpdateSession(session);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpDelete("DeleteSession/{id}")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> DeleteSession(Guid id)
        {
            if (!await CanManageSession(id))
                return Forbid();

            var result = await courseService.DeleteSession(id);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpPost("AddAssignment")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> AddAssignment([FromForm] CreateAssignment assignment)
        {
            if (!await CanManageSession(assignment.SessionId))
                return Forbid();
            
            var result = await courseService.AddAssignment(assignment);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("GetAllAssignments/{courseId}")]
        public async Task<IActionResult> GetCourseAllAssignments(Guid courseId)
        {
            var result = await courseService.GetCourseAllAssignments(courseId);
            return Ok(result);
        }
        [HttpGet("GetAssignmentById/{id}")]
        public async Task<IActionResult> GetAssignmentById(Guid id)
        {
            var result = await courseService.GetAssignmentById(id);
            if (result == null)
                return NotFound(new { success = false, message = "Assignment not found" });
            return Ok(result);
        }
        [HttpGet("GetAssignmentBySession/{sessionId}")]
        public async Task<IActionResult> GetAssignmentBySession(Guid sessionId)
        {
            var result = await courseService.GetAssignmentBySession(sessionId);
            if (result == null)
                return NotFound(new { success = false, message = "Assignment not found" });
            return Ok(result);
        }
        [HttpPut("UpdateAssignment")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> UpdateAssignment([FromForm] UpdateAssignment assignment)
        {
            if (!await CanManageAssignment(assignment.Id))
                return Forbid();

            var result = await courseService.UpdateAssignment(assignment);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpDelete("DeleteAssignment/{id}")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> DeleteAssignment(Guid id)
        {
            if (!await CanManageAssignment(id))
                return Forbid();

            var result = await courseService.DeleteAssignment(id);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }

        private async Task<bool> CanManageCourse(Guid courseId)
        {
            if (User.IsInRole(AppRoles.Admin))
                return true;

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userId) && await courseService.CanManageCourse(courseId, userId);
        }

        private async Task<bool> CanManageSession(Guid sessionId)
        {
            if (User.IsInRole(AppRoles.Admin))
                return true;

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userId) && await courseService.CanManageSession(sessionId, userId);
        }

        private async Task<bool> CanManageAssignment(Guid assignmentId)
        {
            if (User.IsInRole(AppRoles.Admin))
                return true;

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userId) && await courseService.CanManageAssignment(assignmentId, userId);
        }

    }
}
