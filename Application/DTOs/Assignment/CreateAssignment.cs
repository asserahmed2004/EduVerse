using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Assignment
{
    public class CreateAssignment
    {
        public Guid SessionId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public IFormFile File { get; set; }
    }
}
