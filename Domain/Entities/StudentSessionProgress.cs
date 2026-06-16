namespace Domain.Entities
{
    public class StudentSessionProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StudentId { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public Guid SessionId { get; set; }
        public bool IsDone { get; set; }
        public DateTime? DoneAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
