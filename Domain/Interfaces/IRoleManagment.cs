using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Domain.Interfaces
{
    public interface IRoleManagment
    {
        Task<string> GetUserRole(string userEmail);
        Task<IdentityResult> AddUserToRole(AppUser user, string roleName);
        Task<IdentityResult> AddRole(string roleName);
        Task<bool> DeleteRole(string roleName);
        Task<int> CountUsersInRoleAsync(string roleName);
        Task<List<AppUser>> GetUsersInRoleAsync(string roleName);
    }
}
