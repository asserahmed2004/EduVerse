using API.Authorization;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CertificateController(IUserService userService) : ControllerBase
    {
        [HttpPost("Generate/{courseId}")]
        [Authorize(Roles = AppRoles.StudentAccess)]
        public async Task<IActionResult> Generate(Guid courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var result = await userService.GenerateCertificate(courseId, userId, Request.Scheme + "://" + Request.Host);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Eligibility/{courseId}")]
        [Authorize(Roles = AppRoles.StudentAccess)]
        public async Task<IActionResult> Eligibility(Guid courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var eligibility = await userService.GetCertificateEligibility(courseId, userId);
            if (eligibility == null)
                return NotFound(new { success = false, message = "Certificate eligibility not found or student is not enrolled." });

            return Ok(new { success = true, message = "Certificate eligibility retrieved successfully", data = eligibility });
        }

        [HttpGet("Download/{certificateId:guid}")]
        [Authorize(Roles = AppRoles.StudentAccess)]
        public async Task<IActionResult> Download(Guid certificateId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var certificate = await userService.GetCertificateDownload(certificateId, userId);
            if (certificate == null)
                return NotFound(new { success = false, message = "Certificate was not found or does not belong to this student." });

            return File(certificate.Content, "application/pdf", certificate.FileName);
        }

        [HttpGet("Verify/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(string code)
        {
            var result = await userService.VerifyCertificate(code);
            return result.success ? Ok(result) : NotFound(result);
        }
    }
}
