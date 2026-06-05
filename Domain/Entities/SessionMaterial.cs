namespace Domain.Entities
{
    public class SessionMaterial
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Link";
        public string? Url { get; set; }
        public string? FilePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
