namespace Domain.Entities
{

    using System;

    public class Session
    {
        public Guid Id { get; set; }=Guid.NewGuid();
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string FileUrl { get; set; }
        public string TrainerId { get; set; }
        public DateTime Date { get; set; }
        public double Duration { get; set; }
        public int SessionNumber { get; set; }
    }
}
