using API.Authorization;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RecommendationController(IRecommendationService recommendationService) : ControllerBase
    {
        [HttpGet("ForMe")]
        [Authorize(Roles = AppRoles.StudentAccess)]
        public async Task<IActionResult> GetForMe()
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Unauthorized(new { success = false, message = "User id claim is missing." });
            }

            var result = await recommendationService.GetPersonalizedRecommendationsAsync(studentId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Similar/{courseId:guid}")]
        public async Task<IActionResult> GetSimilar(Guid courseId)
        {
            var result = await recommendationService.GetSimilarCoursesAsync(courseId);
            return result.success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("Trending")]
        public async Task<IActionResult> GetTrending()
        {
            var result = await recommendationService.GetTrendingCoursesAsync();
            return result.success ? Ok(result) : BadRequest(result);
        }
    }
}
