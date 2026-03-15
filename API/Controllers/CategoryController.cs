using Application.DTOs.Category;
using Application.Services.Implementitions;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CategoryController(ICategoryService service) : ControllerBase
    {
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await service.GetAllCategories();
            return Ok(categories);
        }
        [HttpGet("GetById/{id}")]

        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await service.GetCategoryById(id);
            if (category == null)
            {
                return NoContent();
            }
            return Ok(category);
        }
        [HttpGet("GetByName/{name}")]
        public async Task<IActionResult> GetCategoryByName(string name)
        {
            var category = await service.GetCategoryByName(name);
            if (category == null)
            {
                return NoContent();
            }
            return Ok(category);
        }
        [HttpPost("Create")]
        public async Task<IActionResult> CreateCategory(CreateCategory category)
        {
            var result = await service.CreateCategory(category);
            if (result.success)
            {
                return Ok(result.message);
            }
            else
            {
                return BadRequest(result.message);
            }

        }
        [HttpPut("Update")]
        public async Task<IActionResult> UpdateCategory(UpdateCategory category)
        {
            var result = await service.UpdateCategory(category);
            if (result.success)
            {
                return Ok(result.message);
            }
            else
            {
                return BadRequest(result.message);
            }
        }
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var result = await service.DeleteCategory(id);
            if (result.success)
            {
                return Ok(result.message);
            }
            else
            {
                return BadRequest(result.message);
            }
        }
    }
}
