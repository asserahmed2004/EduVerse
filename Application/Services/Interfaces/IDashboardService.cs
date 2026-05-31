using Application.DTOs.Responses;

namespace Application.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<ServiceResponse> GetOrganizationStatsAsync(
            string currentUserId,
            bool isAdmin,
            bool isOrganizationAdmin,
            bool isInstructor);
        Task<ServiceResponse> GetOrganizationsOverviewAsync();
        Task<ServiceResponse> GetRecentEnrollmentsAsync();
        Task<ServiceResponse> GetRecentPaymentsAsync();
        Task<ServiceResponse> GetRecentCoursesAsync();
        Task<ServiceResponse> GetTopCoursesAsync();
        Task<ServiceResponse> GetTopOrganizationsAsync();
        Task<ServiceResponse> GetTopInstructorsAsync();
        Task<ServiceResponse> GetOrganizationDetailsAsync(string organizationAdminId);
        Task<ServiceResponse> GetRecentActivitiesAsync();
        Task<ServiceResponse> GetAdminStudentsAsync();
        Task<ServiceResponse> GetAdminInstructorsAsync();
        Task<ServiceResponse> GetRecentSessionsAsync();
        Task<ServiceResponse> GetRecentAssignmentsAsync();
        Task<ServiceResponse> GetTopRatedCoursesAsync();
        Task<ServiceResponse> GetAdminUserDetailsAsync(string userId);
        Task<ServiceResponse> GetRevenueTrendAsync(int days);
        Task<ServiceResponse> GetEnrollmentsTrendAsync(int days);
        Task<ServiceResponse> GetUsersByRoleAsync();
        Task<ServiceResponse> GetTopCoursesChartAsync();
    }
}
