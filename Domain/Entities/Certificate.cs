namespace Domain.Entities
{

    using System;

    public class Certificate
    {
        
        public Guid CourseId { get; set; }
        public string StudentId { get; set; }
        public DateTime GraduationDate { get; set; }
        public string FileUrl { get; set; }
    }
}
