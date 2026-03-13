namespace Domain.Entities
{

    using System;

    public class Enrollment
    {
        
        public Guid CourseId { get; set; }
        public string StudentId { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public double Progression { get; set; }
    }
}
