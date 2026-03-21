using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Assignment
{
    public class UpdateAssignment
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public IFormFile File { get; set; }
    }
}
