using Application.DTOs.Category;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Course
{
    public class GetCourse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public double Price { get; set; }
        public double Duration { get; set; }
        public float Rating { get; set; }
        public float UserRating { get; set; }
        public int RatingCount { get; set; }
        public string OrgId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? InstructorId { get; set; }
        public string ImageUrl { get; set; }
        public List<GetCategory> Categories { get; set; }
        public string? Category { get; set; }
        public string? InstructorName { get; set; }
        public string? OrganizationOwnerName { get; set; }
        public string? OrganizationOwnerEmail { get; set; }
        public int StudentsCount { get; set; }
        public int SessionsCount { get; set; }
        public double ProgressPercent { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedById { get; set; }
        public string? DeletedByName { get; set; }
        public DateTime? RestoredAt { get; set; }
        public string? RestoredById { get; set; }
        public string? RestoredByName { get; set; }
        public string? Tags { get; set; }
        public string? Level { get; set; }
    }
}
