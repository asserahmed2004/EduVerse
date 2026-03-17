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
            var result = await courseService.GetAllCourses();
            return Ok(result);
        }
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetCourseById(Guid id)
        {
            var result = await courseService.GetCourseById(id);
            if (result == null)
                return NotFound(new { success = false, message = "Course not found" });
            return Ok(result);
        }
        [HttpGet("GetByName/{name}")]
        public async Task<IActionResult> GetCourseByName(string name)
        {
            var result = await courseService.GetCourseByName(name);
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
    }
}
