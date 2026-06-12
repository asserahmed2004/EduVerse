namespace Domain.Entities
{
    public class AttendanceRecord
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public bool Attended {  get; set; } = false;
    }
}
