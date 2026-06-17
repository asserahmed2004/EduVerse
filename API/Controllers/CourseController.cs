using Application.DTOs.Assignment;
using Application.DTOs.Course;
using Application.DTOs.Rating;
using Application.DTOs.Responses;
using Application.DTOs.Sessions;
using Application.DTOs.Learning;
using Application.Services.Interfaces.Auth;
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
    public class CourseController(ICourseService courseService, IAuthServices authServices, IMapper mapper) : ControllerBase
    {
        [HttpPost("Create")]
        [Authorize(Roles = AppRoles.AdminOrOrganizationAdmin)]
        public async Task<IActionResult> CreateCourse([FromForm] CreateCourse Course)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "User id claim is missing" });

            var result = await courseService.CreateCourse(Course, userId, User.HasRole(AppRoles.Admin));
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpDelete("Delete/{id}")]
        [Authorize(Roles = AppRoles.AdminOrOrganizationAdmin)]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            return BadRequest(new
            {
                success = false,
                message = "Password is required to delete a course. Use POST /Course/DeleteWithPassword/{id} with a password in the request body."
            });
        }

        [HttpPost("DeleteWithPassword/{id}")]
        [Authorize(Roles = AppRoles.AdminOrOrganizationAdmin)]
        public async Task<IActionResult> DeleteCourseWithPassword(Guid id, [FromBody] DeleteCourseRequest request)
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Unauthorized(new { success = false, message = "You must be logged in to delete a course" });

            if (request == null || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { success = false, message = "Password is required" });

            if (!await courseService.CourseExists(id))
                return NotFound(new { success = false, message = "Course not found" });

            if (await courseService.IsCourseDeleted(id))
                return BadRequest(new { success = false, message = "Course is already deleted" });

            if (!await CanManageCourse(id))
                return Forbid();

            var passwordIsValid = await authServices.VerifyCurrentUserPasswordAsync(User, request.Password);
            if (!passwordIsValid)
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Invalid password" });

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id claim is missing" });

            var profile = await authServices.GetProfile(userId);
            var deletedByName = profile?.FullName
                ?? profile?.UserName
                ?? profile?.Email
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                ?? "Unknown";

            var result = await courseService.DeleteCourse(id, userId, deletedByName);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("Restore/{id}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> RestoreCourse(Guid id)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id claim is missing" });

            var profile = await authServices.GetProfile(userId);
            var restoredByName = profile?.FullName
                ?? profile?.UserName
                ?? profile?.Email
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                ?? "Unknown";

            var result = await courseService.RestoreCourse(id, userId, restoredByName);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("GetDeletedCourses")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetDeletedCourses()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var result = await courseService.GetDeletedCourses(userId);
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

            var result = await courseService.GetAllCourses(
                userId,
                User.HasRole(AppRoles.Admin),
                User.HasRole(AppRoles.OrganizationAdmin),
                User.HasRole(AppRoles.Instructor));
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
            var result = await courseService.GetCourseByCategory(
                category,
                userId,
                User.HasRole(AppRoles.Admin),
                User.HasRole(AppRoles.OrganizationAdmin),
                User.HasRole(AppRoles.Instructor));
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
            var result = await courseService.Search(
                query,
                userId,
                User.HasRole(AppRoles.Admin),
                User.HasRole(AppRoles.OrganizationAdmin),
                User.HasRole(AppRoles.Instructor));
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

        [HttpGet("AdminDetails/{id}")]
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
        public async Task<IActionResult> GetAdminCourseDetails(Guid id)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var result = await courseService.GetAdminCourseDetails(
                id,
                userId,
                User.HasRole(AppRoles.Admin),
                User.HasRole(AppRoles.OrganizationAdmin),
                User.HasRole(AppRoles.Instructor));

            if (result == null)
                return NotFound(new { success = false, message = "Course details not found or access denied" });

            return Ok(new { success = true, message = "Course details retrieved successfully", data = result });
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
        [Authorize(Roles = AppRoles.AdminOrOrganizationAdmin)]
        public async Task<IActionResult> UpdateCourse([FromForm] UpdateCourse Course)
        {
            if (!await CanManageCourse(Course.Id))
                return Forbid();

            var result = await courseService.UpdateCourse(Course);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("AssignInstructor")]
        [Authorize(Roles = AppRoles.AdminOrOrganizationAdmin)]
        public async Task<IActionResult> AssignInstructor([FromBody] AssignInstructorRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id claim is missing" });

            var result = await courseService.AssignInstructor(request.CourseId, request.InstructorId, userId, User.HasRole(AppRoles.Admin));
            return result.success ? Ok(result) : BadRequest(result);
        }
        [HttpPost("AddRating")]
        [Authorize(Roles = AppRoles.StudentAccess)]
        public async Task<IActionResult> AddRating( CreateRating rating)
        {
            string? userId;
            if (User.Identity?.IsAuthenticated == true)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
            else
            {
                return Unauthorized(new { success = false, message = "You must be logged in to add a rating" });
            }
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id claim is missing" });

            var result = await courseService.AddRating(rating, userId);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpPost("AddSession")]
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
        public async Task<IActionResult> AddSession([FromForm]CreateSessionRequest session)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id claim is missing" });

            var sessionEntity = mapper.Map<CreateSession>(session);
            var result = await courseService.AddSession(
                sessionEntity,
                userId,
                User.HasRole(AppRoles.Admin),
                User.HasRole(AppRoles.OrganizationAdmin),
                User.HasRole(AppRoles.Instructor));
            
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpGet("GetAllSessions/{courseId}")]
        public async Task<IActionResult> GetCourseAllSessions(Guid courseId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var result = await courseService.GetCourseAllSessions(courseId, userId);
            
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
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
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
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
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
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
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
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
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
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
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
            if (User.HasRole(AppRoles.Admin))
                return true;

            if (!User.HasRole(AppRoles.OrganizationAdmin))
                return false;

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userId) && await courseService.CanManageCourse(courseId, userId);
        }

        private async Task<bool> CanManageCourseLearningContent(Guid courseId)
        {
            if (User.HasRole(AppRoles.Admin))
                return true;

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return false;

            if (User.HasRole(AppRoles.OrganizationAdmin) && await courseService.CanManageCourse(courseId, userId))
                return true;

            return User.HasRole(AppRoles.Instructor) && await courseService.CanManageAssignedCourse(courseId, userId);
        }

        private async Task<bool> CanManageSession(Guid sessionId)
        {
            if (User.HasRole(AppRoles.Admin))
                return true;

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userId) && await courseService.CanManageSession(sessionId, userId);
        }

        private async Task<bool> CanManageAssignment(Guid assignmentId)
        {
            if (User.HasRole(AppRoles.Admin))
                return true;

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userId) && await courseService.CanManageAssignment(assignmentId, userId);
        }

    }
}
