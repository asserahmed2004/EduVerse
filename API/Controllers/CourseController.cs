using Application.DTOs.Course;
using Application.DTOs.Rating;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CourseController(ICourseService courseService) : ControllerBase
    {
        [HttpPost("Create")]
        public async Task<IActionResult> CreateCourse([FromForm] CreateCourse Course)
        {
            var result = await courseService.CreateCourse(Course);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
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
        public async Task<IActionResult> UpdateCourse([FromForm] UpdateCourse Course)
        {
            var result = await courseService.UpdateCourse(Course);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
        [HttpPost("AddRating")]
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

    }
}
