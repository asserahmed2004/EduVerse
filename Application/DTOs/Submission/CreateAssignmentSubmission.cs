using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Submission
{
    public class CreateAssignmentSubmission
    {
        public string? StudentId { get; set; }
        public Guid AssignmentId { get; set; }
        public IFormFile? File { get; set; }
        public string? TextAnswer { get; set; }

    }
}
