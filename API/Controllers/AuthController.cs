
using Application.DTOs.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using API.Authorization;
using Application.Services.Interfaces.Auth;
using Application.Services.Interfaces;

namespace Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController(IAuthServices authServices, ICloudService cloudService) : ControllerBase
    {
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromForm]RegisterUser registerUser)
        {
            var result = await authServices.RegisterUser(registerUser);
            return result.succeed ? Ok(result) : BadRequest(result);
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginUser loginUser)
        {
            var result = await authServices.LoginUser(loginUser);
            return result.succeed ? Ok(result) : BadRequest(result);
        }
        [HttpPost("ReviveToken")]
        public async Task<IActionResult> ReviveToken([FromBody] string refreshtoken)
        {
            

            var result = await authServices.ReviveToken(refreshtoken);

            return result.succeed ? Ok(result) : BadRequest(result);
        }
        [HttpGet("GetUser")]
        [Authorize]

        public async Task<IActionResult> GetUser()
        {

            if (User.Identity!.IsAuthenticated)
            {
                var header = Request.Headers.Where(h => h.Key == "Authorization");
                var token = header.First().Value.ToString().Replace("Bearer ", "");
                var claims = User.Claims.ToList();


                return Ok(new
                {
                    Role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value,
                    Id = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                    email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                    UserName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                    Token = token
                });
            }
            return Unauthorized();
        }
        [HttpPost("AddRole/{Role}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> AddRole(string Role)
        {
            var result = await authServices.AddRole(Role);
            if (result.success)
                return Ok(result);
            return BadRequest(result);
        }
        [HttpDelete("RemoveRole/{Role}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> RemoveRole(string Role)
        {
            var result = await authServices.RemoveRole(Role);
            if (result.success)
                return Ok(result);
            return BadRequest(result);
        }
        [HttpPost("AddUserToRole/{UserId}/{Role}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> AddUserToRole(string UserId, string Role)
        {
            var performedById = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var performedByName =
                User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ??
                User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ??
                User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ??
                "Admin";

            var result = await authServices.AddUserToRole(UserId, Role, performedById, performedByName);
            if (result.success)
                return Ok(result);
            return BadRequest(result);
        }
        [HttpPost("SendConfirmationEmail/{email}")]
        public async Task<IActionResult> SendConfirmationEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { success = false, message = "Email is required" });

            var result = await authServices.SendConfirmationEmail(email);
            if (result?.ConfirmationCode == null)
                return BadRequest(new { success = false, message = "Could not send the confirmation code. Check the email address and mail configuration." });

            return Ok(new { success = true, message = "Confirmation code sent to your email." });
        }
        [HttpGet("GetAllUsers/{Role?}")]
        [Authorize(Roles = AppRoles.AdminAccess)]
        public async Task<IActionResult> GetAllUsers(string? Role)
        {
            var result = await authServices.GetAllUsers(Role);
            if (result != null)
                return Ok(result);
            return BadRequest(result);

        }
        [HttpGet("GetProfile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var result = await authServices.GetProfile(userId);
                if (result != null)
                    return Ok(new
                    {
                        success = true,
                        message = "Profile retrieved successfully",
                        data = result
                    });
                return NotFound(new
                {
                    success = false,
                    message = "User not found"
                });
            }
            return Unauthorized();
        }

        [HttpPut("UpdateProfile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User is not authenticated" });

            var result = await authServices.UpdateProfile(userId, request);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User is not authenticated" });

            var result = await authServices.ChangePassword(userId, request);
            return result.success ? Ok(result) : BadRequest(result);
        }
       
    }
}
