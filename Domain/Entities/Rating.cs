namespace Domain.Entities
{

    using System;

    public class Rating
    {
        
        public string StudentId { get; set; }
        public Guid CourseId { get; set; }
        public float RatingValue { get; set; }
    }
}
