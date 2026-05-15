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
using System.Text.Json;

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
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id was not found in token.");
            }
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
            var tokenUserId = GetUserId();
            if (string.IsNullOrEmpty(tokenUserId))
            {
                return Unauthorized("User id was not found in token.");
            }

            if (User.IsInRole(AppRoles.Student) || string.IsNullOrEmpty(userid))
            {
                userid = tokenUserId;
            }

            var courses = await userService.GetEnrolledCourses(userid);

            
            
            return Ok(courses);

        }

        [HttpGet("my-enrolled-courses")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> myenrolledcourses()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id was not found in token.");
            }

            var courses = await userService.GetEnrolledCourses(userId);
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
            Email = ResolveRequestedEmail(Email);
            if (string.IsNullOrEmpty(Email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var fileUrl = await userService.GetCertificateFile(courseId, Email);
            if (string.IsNullOrEmpty(fileUrl))
            {
                return NotFound("Certificate file not found.");
            }
            return Ok(fileUrl);
        }

        [HttpGet("my-certificate/{courseId}")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> mycertificatefile(Guid courseId)
        {
            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var fileUrl = await userService.GetCertificateFile(courseId, email);
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
            Email = ResolveRequestedEmail(Email);
            if (string.IsNullOrEmpty(Email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var certificates = await userService.GetUserCertificates(Email);
            return Ok(certificates);
        }

        [HttpGet("my-certificates")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> mycertificates()
        {
            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var certificates = await userService.GetUserCertificates(email);
            return Ok(certificates);
        }

        [HttpGet("enrollmentdata/{courseId}/{Email?}")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> enrollmentdata(Guid courseId, string? Email)
        {
            Email = ResolveRequestedEmail(Email);
            if (string.IsNullOrEmpty(Email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var enrollment = await userService.GetEnrollmentData(courseId, Email);
            if (enrollment == null)
            {
                return NotFound("Enrollment data not found.");
            }
            return Ok(enrollment);
        }

        [HttpGet("my-enrollment/{courseId}")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> myenrollmentdata(Guid courseId)
        {
            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var enrollment = await userService.GetEnrollmentData(courseId, email);
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
            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email was not found in token.");
            }

            progression.Email = email;
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
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id was not found in token.");
            }

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
            Email = ResolveRequestedEmail(Email);
            if (string.IsNullOrEmpty(Email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var submissions = await userService.GetUserSubmissions(Email);
            return Ok(submissions);
        }

        [HttpGet("my-submissions")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> mysubmissions()
        {
            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var submissions = await userService.GetUserSubmissions(email);
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
            Email = ResolveRequestedEmail(Email);
            if (string.IsNullOrEmpty(Email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var submission = await userService.GetSubmission(Id, Email);
            if (submission == null)
            {
                return NotFound("Submission not found.");
            }
            return Ok(submission);
        }

        [HttpGet("my-submission/{Id}")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> mysubmission(Guid Id)
        {
            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email was not found in token.");
            }

            var submission = await userService.GetSubmission(Id, email);
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
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id was not found in token.");
            }

            var result = await userService.Payment(userId, CourseId, Method);
            if (String.IsNullOrEmpty(result))
            {
                return BadRequest("Payment request failed. Check the course id, payment method, or payment provider response.");
            }
            return Ok(result);
        }

        [HttpGet("payments")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> payments()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id was not found in token.");
            }

            var payments = await userService.GetUserPayments(userId);
            return Ok(payments);
        }

        [HttpGet("payments/{courseId}")]
        [Authorize(Roles = AppRoles.Student)]
        public async Task<IActionResult> paymentdetails(Guid courseId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id was not found in token.");
            }

            var payment = await userService.GetPayment(courseId, userId);
            if (payment == null)
            {
                return NotFound("Payment not found.");
            }

            return Ok(payment);
        }

        [HttpGet("payments/course/{courseId}")]
        [Authorize(Roles = AppRoles.AdminOrInstructor)]
        public async Task<IActionResult> coursepayments(Guid courseId)
        {
            var payments = await userService.GetCoursePayments(courseId);
            return Ok(payments);
        }

        [HttpPost("payment/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> paymentcallback([FromBody] JsonElement callbackData)
        {
            var result = await userService.UpdatePaymentFromCallback(callbackData);
            if (!result.success)
            {
                return BadRequest(result.message);
            }

            return Ok(result.message);
        }

        private string? GetUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        private string? GetUserEmail()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        private string? ResolveRequestedEmail(string? requestedEmail)
        {
            var tokenEmail = GetUserEmail();

            if (User.IsInRole(AppRoles.Student))
            {
                return tokenEmail;
            }

            return string.IsNullOrEmpty(requestedEmail) ? tokenEmail : requestedEmail;
        }
        //Task<ServiceResponse> SubmitAssignment(CreateAssignmentSubmission submission);
        //Task<ServiceResponse> UpdateAssignmentSubmission(UpdateAssignmentSubmission submission);
        //Task<IEnumerable<GetAssignmentSubmission>> GetUserSubmissions(string Email);
        //Task<IEnumerable<GetAssignmentSubmission>> GetAssignmentSubmissions(Guid Id);

        //Task<GetAssignmentSubmission> GetSubmission(Guid Id, string Email);











    }
}
