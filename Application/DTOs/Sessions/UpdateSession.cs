using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Sessions
{
    public class UpdateSession
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public IFormFile File { get; set; }
        public string TrainerId { get; set; }
        public DateTime Date { get; set; }
        public double Duration { get; set; }
        public int SessionNumber { get; set; }
    }
}
