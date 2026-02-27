using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ITokenManagment
    {
        string GetRefreshTokenAsync();
        Task<List<Claim>> GetUserClaimsFromToken(string token);
        Task<bool> ValidateRefreshToken(string refreshToken);
        Task<string> GetUserIdFromToken(string token);
        Task<int> AddRefreshToken(string userId, string refreshToken);
        Task<int> RemoveRefreshToken(string userId);
        Task <int> UpdateRefreshToken(string userId, string refreshToken);
        string GenerateToken(List<Claim> claims);
    }
}
