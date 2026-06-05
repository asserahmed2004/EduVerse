using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Submission
{
    public class GetAssignmentSubmission
    {
        public string StudentId { get; set; }
        public Guid AssignmentId { get; set; }
        public string FileUrl { get; set; }
        public string? TextAnswer { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public double? Grade { get; set; }
        public string? Feedback { get; set; }
        public bool IsLate { get; set; }
    }
}
