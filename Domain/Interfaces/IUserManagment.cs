using Domain.Entities;
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
        Task<string> RegisterUser(AppUser user);
        Task<AppUser?> GetUserByEmail(string email);
        Task<bool> LoginUser(AppUser user);
        Task<AppUser?> GetUserById(string userId);
        Task<IEnumerable<AppUser>> GetAllUsers();
        Task<int> RemoveUser(string email);
        Task<List<Claim>> GetUserClaims(string email);

    }
}
