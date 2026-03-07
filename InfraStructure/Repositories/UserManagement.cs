
using InfraStructure.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Domain.Interfaces;
using Domain.Entities;

namespace InfraStructure.Repositories
{
    public class UserManagement(UserManager<AppUser> userManager, IRoleManagment roleManagment, AppDbContext context) : IUserManagment
    {
        public async Task<IEnumerable<AppUser>> GetAllUsers()
        {
           return await context.Users.ToListAsync();
        }

        public async Task<AppUser?> GetUserByEmail(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }

        public async Task<AppUser?> GetUserById(string userId)
        {
            return await userManager.FindByIdAsync(userId);
        }

        public async Task<List<Claim>> GetUserClaims(string email)
        {
            var user = await GetUserByEmail(email);
            if (user == null)
                return new List<Claim>();


            var role = await roleManagment.GetUserRole(email);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, role),
                new Claim("FullName", user.FullName)
            };
            return claims;

        }

        public async Task<bool> LoginUser(AppUser user)
        {
            var existUser = await GetUserByEmail(user.Email);
            if (existUser == null)
                return false;
            var role = await roleManagment.GetUserRole(user.Email);
            
            var result = await userManager.CheckPasswordAsync(existUser, user.PasswordHash);
            return result;
        }

        public async Task<bool> RegisterUser(AppUser user)
        {
            var Exist = await GetUserByEmail(user.Email);
            if (Exist != null)
                return false;
            var result = await userManager.CreateAsync(user, user.PasswordHash);
            var error = result.Errors;

            return result.Succeeded;

        }
        public async Task<bool> UpdateUser(AppUser user)
        {
            if (user == null)
                return false;
            var result = await userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<int> RemoveUser(string email)
        {
            var user = await GetUserByEmail(email);
            if (user == null)
                return 0;
            context.Users.Remove(user);
            return await context.SaveChangesAsync();


        }
    }
}
