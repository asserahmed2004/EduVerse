using Application.DTOs.Responses;

namespace Application.Services.Interfaces
{
    public interface IAdminService
    {
        Task<ServiceResponse> GlobalSearchAsync(string? query);
        Task<ServiceResponse> GetUserDetailsAsync(string userId);
    }
}
