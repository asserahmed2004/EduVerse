using Application.DTOs.Dashboard;

namespace Application.DTOs.Admin
{
    public class ActivityLogDto
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ActivityLogQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public string? Search { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class GlobalSearchResultDto
    {
        public IEnumerable<SearchUserDto> Users { get; set; } = [];
        public IEnumerable<SearchCourseDto> Courses { get; set; } = [];
        public IEnumerable<SearchOrganizationDto> Organizations { get; set; } = [];
    }

    public class SearchUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class SearchCourseDto
    {
        public Guid CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }

    public class SearchOrganizationDto
    {
        public string OrganizationAdminId { get; set; } = string.Empty;
        public string OrganizationAdminName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UserActivityDetailsDto : AdminUserDetailsDto
    {
        public string CreatedAt { get; set; } = "Not available";
        public string LastLogin { get; set; } = "Not available";
        public IEnumerable<ActivityLogDto> RecentActivityLogs { get; set; } = [];
    }
}
