
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
        public IQueryable<AppUser> QueryUsers(bool tracking = false)
        {
            return tracking ? context.Users : context.Users.AsNoTracking();
        }

        public async Task<IEnumerable<AppUser>> GetAllUsers()
        {
           return await QueryUsers().ToListAsync();
        }

        public async Task<AppUser?> GetUserByEmail(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }

        public async Task<AppUser?> GetUserById(string userId)
        {
            return await userManager.FindByIdAsync(userId);
        }

        public async Task<bool> CheckPassword(AppUser user, string password)
        {
            if (user == null || string.IsNullOrWhiteSpace(password))
                return false;

            return await userManager.CheckPasswordAsync(user, password);
        }

        public async Task<IdentityResult> ChangePassword(AppUser user, string currentPassword, string newPassword)
        {
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found" });

            return await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
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

        public async Task<IdentityResult> RegisterUser(AppUser user)
        {
            var Exist = await GetUserByEmail(user.Email);
            if (Exist != null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "Email already exists"
                });
            }

            var result = await userManager.CreateAsync(user, user.PasswordHash);
            return result;

        }
        public async Task<IdentityResult> UpdateUser(AppUser user)
        {
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found" });

            return await userManager.UpdateAsync(user);
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
