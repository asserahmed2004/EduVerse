
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InfraStructure.Repositories
{
    public class RoleMangment(UserManager<AppUser> userManager , RoleManager<IdentityRole> roleManager) : IRoleManagment
    {
        public async Task<bool> AddRole(string roleName)
        {
            var test = roleManager.RoleExistsAsync(roleName);
            if (!test.Result)
            {
                var role = new IdentityRole(roleName);
                var roleResult = await roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                {
                    return false ;
                }
                
            }
            return true;

        }

        public async Task<bool> AddUserToRole(AppUser user, string roleName)
        {
            var test =await roleManager.RoleExistsAsync(roleName);
            if (!test)
            {
                //var roleResult = await roleManager.CreateAsync(new AppUser { UserName = roleName });
                var role = new IdentityRole(roleName);
                var roleResult = await roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                {
                    return false;
                }
            }
            return (await userManager.AddToRoleAsync(user, roleName)).Succeeded;
        }

        public async Task<bool> DeleteRole(string roleName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return false;
            }
            var result = await roleManager.DeleteAsync(role);
            return result.Succeeded;

        }

        public async Task<string> GetUserRole(string userEmail)
        {
            var user = await userManager.FindByEmailAsync(userEmail);
            var role = await userManager.GetRolesAsync(user);
            return role.FirstOrDefault() ?? string.Empty;

        }
    }
}
