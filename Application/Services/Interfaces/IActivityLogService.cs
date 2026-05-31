using Application.DTOs.Admin;
using Application.DTOs.Responses;

namespace Application.Services.Interfaces
{
    public interface IActivityLogService
    {
        Task LogAsync(string? userId, string userName, string action, string entityType, string? entityId, string description);
        Task<ServiceResponse> GetLogsAsync(ActivityLogQuery query);
        Task<IEnumerable<ActivityLogDto>> GetLatestAsync(int count);
        Task<IEnumerable<ActivityLogDto>> GetByUserAsync(string userId, int count);
    }
}
