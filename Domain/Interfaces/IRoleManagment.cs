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
    }
}
