using Application.DTOs.Organization;
using Application.DTOs.Responses;

namespace Application.Services.Interfaces
{
    public interface IOrganizationService
    {
        Task<ServiceResponse> GetAllAsync();
        Task<ServiceResponse> GetByIdAsync(Guid id, string? currentUserId, bool isAdmin, bool isOrganizationAdmin, bool isInstructor);
        Task<ServiceResponse> CreateAsync(CreateOrganizationRequest request, string? createdById, string? createdByName);
        Task<ServiceResponse> UpdateAsync(Guid id, UpdateOrganizationRequest request, string? updatedById, string? updatedByName);
        Task<ServiceResponse> SuspendAsync(Guid id, string? userId, string? userName);
        Task<ServiceResponse> ActivateAsync(Guid id, string? userId, string? userName);
        Task<ServiceResponse> AssignAdminAsync(AssignOrganizationUserRequest request, string? performedById, string? performedByName);
        Task<ServiceResponse> AssignInstructorAsync(AssignOrganizationUserRequest request, string? performedById, string? performedByName);
    }
}
