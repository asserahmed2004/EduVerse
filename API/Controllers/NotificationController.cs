using API.Authorization;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class NotificationController(IUserService userService) : ControllerBase
    {
        [HttpGet("MyNotifications")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> MyNotifications()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            return Ok(new { success = true, message = "Notifications retrieved successfully", data = await userService.GetMyNotifications(userId) });
        }

        [HttpPost("MarkAsRead/{id}")]
        [Authorize(Roles = AppRoles.All)]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { success = false, message = "User id was not found in token." });

            var result = await userService.MarkNotificationAsRead(id, userId);
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
