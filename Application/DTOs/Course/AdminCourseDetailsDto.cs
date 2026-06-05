using Application.DTOs.Assignment;
using Application.DTOs.Payment;
using Application.DTOs.Sessions;

namespace Application.DTOs.Course
{
    public class AdminCourseDetailsDto
    {
        public Guid CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? OrganizationOwner { get; set; }
        public string? OrganizationOwnerEmail { get; set; }
        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? InstructorId { get; set; }
        public string? InstructorName { get; set; }
        public double Price { get; set; }
        public string? ImageUrl { get; set; }
        public int StudentsCount { get; set; }
        public int SessionsCount { get; set; }
        public double AverageRating { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedById { get; set; }
        public string? DeletedByName { get; set; }
        public DateTime? RestoredAt { get; set; }
        public string? RestoredById { get; set; }
        public string? RestoredByName { get; set; }
        public IEnumerable<GetSession> Sessions { get; set; } = [];
        public IEnumerable<AdminCourseStudentDto> Students { get; set; } = [];
        public IEnumerable<GetAssignment> Assignments { get; set; } = [];
        public IEnumerable<AdminPaymentTransactionDto> RecentPayments { get; set; } = [];
    }

    public class AdminCourseStudentDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public double Progression { get; set; }
    }
}
