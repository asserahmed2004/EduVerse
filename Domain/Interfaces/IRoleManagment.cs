using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IRoleManagment
    {
        Task<string> GetUserRole(string userEmail);
        Task<bool> AddUserToRole(AppUser user , string roleName);
        Task<bool> AddRole(string roleName);
        Task<bool> DeleteRole(string roleName);
    }
}
