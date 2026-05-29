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
    }
}
