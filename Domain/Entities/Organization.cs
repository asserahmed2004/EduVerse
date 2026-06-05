using System;

namespace Domain.Entities
{
    public class Organization
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LogoUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
    }
}
