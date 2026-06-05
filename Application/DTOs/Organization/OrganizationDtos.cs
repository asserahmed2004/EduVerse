namespace Application.DTOs.Organization
{
    public class CreateOrganizationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WebsiteUrl { get; set; }
    }

    public class UpdateOrganizationRequest : CreateOrganizationRequest
    {
    }

    public class AssignOrganizationUserRequest
    {
        public Guid OrganizationId { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    public class OrganizationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? LogoUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public int CoursesCount { get; set; }
        public int StudentsCount { get; set; }
        public int EnrollmentsCount { get; set; }
        public double Revenue { get; set; }
        public double AverageRating { get; set; }
        public IEnumerable<OrganizationUserDto> Admins { get; set; } = [];
        public IEnumerable<OrganizationUserDto> Instructors { get; set; } = [];
    }

    public class OrganizationUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
