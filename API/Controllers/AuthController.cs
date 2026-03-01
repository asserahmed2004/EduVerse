
using Application.DTOs.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using Application.Services.Interfaces.Auth;

namespace Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController(IAuthServices authServices) : ControllerBase
    {
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterUser registerUser)
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
        [HttpPost("ReviveToken/{refreshtoken}")]
        public async Task<IActionResult> ReviveToken(string refreshtoken)
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
                    email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                    UserName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                    Token = token
                });
            }
            return Unauthorized();
        }
        [HttpPost("AddRole/{Role}")]
        [Authorize]
        public async Task<IActionResult> AddRole(string Role)
        {
            var result = await authServices.AddRole(Role);
            if (result.success)
                return Ok(result);
            return BadRequest(result);
        }
        [HttpDelete("RemoveRole/{Role}")]
        [Authorize]
        public async Task<IActionResult> RemoveRole(string Role)
        {
            var result = await authServices.RemoveRole(Role);
            if (result.success)
                return Ok(result);
            return BadRequest(result);
        }
        [HttpPost("AddUserToRole/{UserId}/{Role}")]
        [Authorize]
        public async Task<IActionResult> AddUserToRole(string UserId, string Role)
        {
            var result = await authServices.AddUserToRole(UserId, Role);
            if (result.success)
                return Ok(result);
            return BadRequest(result);
        }
        [HttpPost("SendConfirmationEmail/{email}")]
        public async Task<IActionResult> SendConfirmationEmail(string email)
        {
            var result = await authServices.SendConfirmationEmail(email);
            if (result!=null)
                return Ok(result);
            return BadRequest(result);
        }

    }
}
