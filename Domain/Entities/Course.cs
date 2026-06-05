using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{

    using System;

    public class Course
    {
        public Guid Id { get; set; }= Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public double Price { get; set; }
        public double Duration { get; set; }
        public string ImageUrl { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedById { get; set; }
        public string? DeletedByName { get; set; }
        public DateTime? RestoredAt { get; set; }
        public string? RestoredById { get; set; }
        public string? RestoredByName { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? InstructorId { get; set; }
        
        public string OrgId { get; set; }
    }
}
