namespace Domain.Entities
{

    using System;

    public class Assignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
