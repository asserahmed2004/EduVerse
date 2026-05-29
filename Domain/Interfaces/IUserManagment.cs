using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUserManagment
    {
        Task<IdentityResult> RegisterUser(AppUser user);
        Task<AppUser?> GetUserByEmail(string email);
        Task<bool> LoginUser(AppUser user);
        Task<AppUser?> GetUserById(string userId);
        Task<bool> CheckPassword(AppUser user, string password);
        Task<IEnumerable<AppUser>> GetAllUsers();
        Task<int> RemoveUser(string email);
        Task<List<Claim>> GetUserClaims(string email);

    }
}
