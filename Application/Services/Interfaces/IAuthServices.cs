
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Responses;
using Application.DTOs.Auth;

namespace Application.Services.Interfaces.Auth
{
    public interface IAuthServices
    {
        Task<GetUser> GetProfile(string userId);
        Task<IEnumerable<GetUser>> GetAllUsers(string? roleName);
        Task<LoginResponse> RegisterUser(RegisterUser user);
        Task<LoginResponse> LoginUser(LoginUser user);
        Task<LoginResponse> ReviveToken(string refreshtoken);
        Task<ServiceResponse> AddRole(string roleName); 
        Task<ServiceResponse> RemoveRole(string roleName);
        Task<ServiceResponse> AddUserToRole(string UserId, string roleName);
        Task<ConfirmEmail> SendConfirmationEmail(string email);

    }

}
