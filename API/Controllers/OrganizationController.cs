using API.Authorization;
using Application.DTOs.Organization;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OrganizationController(IOrganizationService organizationService, IAuthServices authServices) : ControllerBase
    {
        [HttpGet("GetAll")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetAll()
        {
            var result = await organizationService.GetAllAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GetById/{id}")]
        [Authorize(Roles = AppRoles.AdminOrganizationAdminOrInstructor)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await organizationService.GetByIdAsync(
                id,
                CurrentUserId(),
                User.HasRole(AppRoles.Admin),
                User.HasRole(AppRoles.OrganizationAdmin),
                User.HasRole(AppRoles.Instructor));

            if (!result.success && result.message?.Contains("not allowed", StringComparison.OrdinalIgnoreCase) == true)
                return Forbid();

            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("Create")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request)
        {
            var result = await organizationService.CreateAsync(request, CurrentUserId(), await CurrentDisplayNameAsync());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("Update/{id}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationRequest request)
        {
            var result = await organizationService.UpdateAsync(id, request, CurrentUserId(), await CurrentDisplayNameAsync());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("Suspend/{id}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> Suspend(Guid id)
        {
            var result = await organizationService.SuspendAsync(id, CurrentUserId(), await CurrentDisplayNameAsync());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("Activate/{id}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> Activate(Guid id)
        {
            var result = await organizationService.ActivateAsync(id, CurrentUserId(), await CurrentDisplayNameAsync());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("AssignAdmin")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> AssignAdmin([FromBody] AssignOrganizationUserRequest request)
        {
            var result = await organizationService.AssignAdminAsync(request, CurrentUserId(), await CurrentDisplayNameAsync());
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("AssignInstructor")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> AssignInstructor([FromBody] AssignOrganizationUserRequest request)
        {
            var result = await organizationService.AssignInstructorAsync(request, CurrentUserId(), await CurrentDisplayNameAsync());
            return result.success ? Ok(result) : BadRequest(result);
        }

        private string? CurrentUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task<string> CurrentDisplayNameAsync()
        {
            var userId = CurrentUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var profile = await authServices.GetProfile(userId);
                return profile?.FullName
                    ?? profile?.UserName
                    ?? profile?.Email
                    ?? "Unknown";
            }

            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                ?? "Unknown";
        }
    }
}
