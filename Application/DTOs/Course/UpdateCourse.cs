using Application.DTOs.Category;
using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Course
{
    public class UpdateCourse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public double Price { get; set; }
        public int Duration { get; set; }
        public IFormFile Image { get; set; }
        public string Categories { get; set; }
    }
}
