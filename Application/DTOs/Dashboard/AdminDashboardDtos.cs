namespace Application.DTOs.Dashboard
{
    public class OrganizationOverviewDto
    {
        public string OrganizationAdminId { get; set; } = string.Empty;
        public string OrganizationAdminName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CoursesCount { get; set; }
        public int StudentsCount { get; set; }
        public int EnrollmentsCount { get; set; }
        public double Revenue { get; set; }
        public double AverageRating { get; set; }
    }

    public class RecentEnrollmentDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public double Progression { get; set; }
    }

    public class RecentPaymentDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime SubmittingDate { get; set; }
        public double TotalPrice { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? MerchantOrderId { get; set; }
        public string? SpecialReference { get; set; }
    }

    public class RecentCourseDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OrganizationAdminId { get; set; } = string.Empty;
        public string OrganizationAdminName { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class TopCourseDto
    {
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OrganizationAdminName { get; set; } = string.Empty;
        public int StudentsCount { get; set; }
        public int SessionsCount { get; set; }
        public double Revenue { get; set; }
        public double AverageRating { get; set; }
    }

    public class TopOrganizationDto
    {
        public string OrganizationAdminId { get; set; } = string.Empty;
        public string OrganizationAdminName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CoursesCount { get; set; }
        public int EnrollmentsCount { get; set; }
        public double Revenue { get; set; }
        public double AverageRating { get; set; }
    }

    public class TopInstructorDto
    {
        public string InstructorId { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int SessionsCount { get; set; }
        public int StudentsCount { get; set; }
        public int CoursesCount { get; set; }
    }

    public class OrganizationDetailsDto : OrganizationOverviewDto
    {
        public IEnumerable<OrganizationCourseDto> Courses { get; set; } = [];
        public IEnumerable<RecentEnrollmentDto> RecentEnrollments { get; set; } = [];
        public IEnumerable<RecentPaymentDto> RecentPayments { get; set; } = [];
    }

    public class OrganizationCourseDto
    {
        public Guid CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StudentsCount { get; set; }
        public int SessionsCount { get; set; }
        public double AverageRating { get; set; }
    }

    public class RecentActivityDto
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminUserDetailsDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int CoursesCount { get; set; }
        public int SessionsCount { get; set; }
        public int EnrollmentsCount { get; set; }
    }

    public class AdminSessionDto
    {
        public Guid SessionId { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int SessionNumber { get; set; }
    }

    public class AdminAssignmentDto
    {
        public Guid AssignmentId { get; set; }
        public Guid SessionId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
