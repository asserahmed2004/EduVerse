using Application.DTOs.Course;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CourseController(ICourseService courseService) : ControllerBase
    {
        [HttpPost("CreateCourse")]
        public async Task<IActionResult> CreateCourse([FromForm] CreateCourse Course)
        {
            var result = await courseService.CreateCourse(Course);
            if (!result.success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
