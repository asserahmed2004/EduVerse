using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Sessions
{
    public class CreateSession
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public IFormFile? File { get; set; }
        public string TrainerId { get; set; }
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? ExternalLink { get; set; }
        public int SessionNumber { get; set; }

    }
    public class CreateSessionRequest
    {
        public string Course { get; set; }
        public string Title { get; set; }
        public IFormFile? File { get; set; }
        public string TrainerId { get; set; }
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? ExternalLink { get; set; }
        public int SessionNumber { get; set; }
    }
}
