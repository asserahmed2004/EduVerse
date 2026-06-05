using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfraStructure.Repositories
{
    public class RoleMangment(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager) : IRoleManagment
    {
        public async Task<IdentityResult> AddRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return IdentityResult.Failed(new IdentityError { Description = "Role name is required" });
            }

            var existingRoleName = await ResolveRoleNameAsync(roleName);
            if (existingRoleName != null)
            {
                return IdentityResult.Success;
            }

            return await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        public async Task<IdentityResult> AddUserToRole(AppUser user, string roleName)
        {
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            var exactRoleName = await ResolveRoleNameAsync(roleName);
            if (exactRoleName == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = $"Role '{roleName}' does not exist" });
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Any(role => string.Equals(role, exactRoleName, StringComparison.OrdinalIgnoreCase)))
            {
                return IdentityResult.Failed(new IdentityError { Description = $"User already has role '{exactRoleName}'" });
            }

            return await userManager.AddToRoleAsync(user, exactRoleName);
        }

        public async Task<bool> DeleteRole(string roleName)
        {
            var exactRoleName = await ResolveRoleNameAsync(roleName);
            if (exactRoleName == null)
            {
                return false;
            }

            var role = await roleManager.FindByNameAsync(exactRoleName);
            if (role == null)
            {
                return false;
            }

            var result = await roleManager.DeleteAsync(role);
            return result.Succeeded;
        }

        public async Task<string> GetUserRole(string userEmail)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return string.Empty;
            }

            var user = await userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return string.Empty;
            }

            var role = await userManager.GetRolesAsync(user);
            return role.FirstOrDefault() ?? string.Empty;
        }

        private async Task<string?> ResolveRoleNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return null;
            }

            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                return role.Name;
            }

            return (await roleManager.Roles.ToListAsync())
                .FirstOrDefault(role => string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase))
                ?.Name;
        }
    }
}
