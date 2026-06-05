namespace Domain.Entities
{
    public class CertificateRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StudentId { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CertificateCode { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Valid";
    }
}
