namespace Domain.Entities
{
    public class AttendanceRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string AttendanceCode { get; set; } = string.Empty;
        public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
    }
}
