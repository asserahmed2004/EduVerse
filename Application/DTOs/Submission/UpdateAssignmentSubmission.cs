using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Submission
{
    public class UpdateAssignmentSubmission
    {
        public string? StudentId { get; set; }
        public Guid AssignmentId { get; set; }
        public IFormFile File { get; set; }
    }
}
