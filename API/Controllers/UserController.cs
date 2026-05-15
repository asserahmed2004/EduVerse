using Application.DTOs.Auth;
using Application.DTOs.Course;
using Application.DTOs.Enrollments;
using Application.DTOs.Responses;
using Application.DTOs.Submission;
using Application.Services.Interfaces;
using API.Authorization;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController(IUserService userService) : ControllerBase
    {
        [HttpPost("enroll/{courseId}")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> Enroll(Guid courseId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var result = await userService.Enroll(courseId, userId);
            if (!result.success)
            {
                return BadRequest(result.message);
            }
            return Ok(result.message);
        }
        [HttpPost("addcertificate")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> AddCertificate([FromForm]CreateCertificate certificate)
        {
            var result = await userService.AddCertificate(certificate);
            if (!result.success)
            {
                return BadRequest(result.message);
            }
            return Ok(result.message);

        }
        [HttpGet("enrolledcourses/{userid?}")]
        [Authorize]
        public async Task<IActionResult> enrolledcourses(string? userid)
        {
            if (string.IsNullOrEmpty(userid))
            {
                userid = User.Claims.FirstOrDefault(c => c.Type ==ClaimTypes.NameIdentifier)?.Value;
            }
            var courses = await userService.GetEnrolledCourses(userid);

            
            
            return Ok(courses);

        }
        [HttpGet("enrolledusers/{courseId}")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> enrolledusers(Guid courseId)
        {
            var users = await userService.GetEnrolledUsers(courseId);
            return Ok(users);
        }
        [HttpGet("certificatefile/{courseId}/{Email}")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> certificatefile(Guid courseId, string Email)
        {
            var fileUrl = await userService.GetCertificateFile(courseId, Email);
            if (string.IsNullOrEmpty(fileUrl))
            {
                return NotFound("Certificate file not found.");
            }
            return Ok(fileUrl);
        }
        [HttpGet("usercertificates/{Email}")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> usercertificates(string Email)
        {
            var certificates = await userService.GetUserCertificates(Email);
            return Ok(certificates);
        }
        [HttpGet("enrollmentdata/{courseId}/{Email?}")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> enrollmentdata(Guid courseId, string? Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            }
            var enrollment = await userService.GetEnrollmentData(courseId, Email);
            if (enrollment == null)
            {
                return NotFound("Enrollment data not found.");
            }
            return Ok(enrollment);
        }
        [HttpPut("updateprogress")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> updateprogress([FromBody]Progression progression)
        {
            var result = await userService.UpdateProgress(progression.CourseId, progression.Email, progression.ProgressionValue);
            if (!result.success)
            {
                return BadRequest(result.message);
            }
            return Ok(result.message);
        }
        [HttpPost("submitassignment")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> submitassignment([FromForm] CreateAssignmentSubmission submission)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            submission.StudentId = userId;
            var result = await userService.SubmitAssignment(submission);
            if (!result.success)
            {
                return BadRequest(result.message);
            }
            return Ok(result.message);
        }
        
        [HttpGet("usersubmissions/{Email?}")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> usersubmissions(string? Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            }
            var submissions = await userService.GetUserSubmissions(Email);
            return Ok(submissions);
        }
        [HttpGet("assignmentsubmissions/{Id}")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> assignmentsubmissions(Guid Id)
        {
            var submissions = await userService.GetAssignmentSubmissions(Id);
            return Ok(submissions);
        }
        [HttpGet("submission/{Id}/{Email?}")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> submission(Guid Id, string? Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                Email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            }
            var submission = await userService.GetSubmission(Id, Email);
            if (submission == null)
            {
                return NotFound("Submission not found.");
            }
            return Ok(submission);
        }
        [HttpPost("payment/{CourseId}/{Method}")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> payment(Guid CourseId, string Method)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var result = await userService.Payment(userId, CourseId, Method);
            if (String.IsNullOrEmpty(result))
            {
                return BadRequest();
            }
            return Ok(result);
        }
        //Task<ServiceResponse> SubmitAssignment(CreateAssignmentSubmission submission);
        //Task<ServiceResponse> UpdateAssignmentSubmission(UpdateAssignmentSubmission submission);
        //Task<IEnumerable<GetAssignmentSubmission>> GetUserSubmissions(string Email);
        //Task<IEnumerable<GetAssignmentSubmission>> GetAssignmentSubmissions(Guid Id);

        //Task<GetAssignmentSubmission> GetSubmission(Guid Id, string Email);











    }
}
