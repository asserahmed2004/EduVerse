namespace Domain.Entities
{

    using System;

    public class Payment
    {
       
        public Guid CourseId { get; set; }
        public string StudentId { get; set; }
        public DateTime SubmittingDate { get; set; }
        public double TotalPrice { get; set; }
    }
}
