namespace Domain.Entities
{

    using System;

    public class AssignmentSubmission
    {
        
        public string StudentId { get; set; }
        public Guid AssignmentId { get; set; }
        public string FileUrl { get; set; }
    }
}
