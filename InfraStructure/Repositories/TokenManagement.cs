
using InfraStructure.Data;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Domain.Interfaces;
using Domain.Entities;

namespace InfraStructure.Repositories
{
    public class TokenManagement(AppDbContext context, IConfiguration configuration) : ITokenManagment
    {
        public async Task<int> AddRefreshToken(string userId, string refreshToken)
        {
            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,

            });
            return await context.SaveChangesAsync();
        }

        public string GenerateToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddDays(1);
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds
                );
            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        public string GetRefreshTokenAsync()
        {
            const int tokenLength = 64;
            byte[] randomNumber = new byte[tokenLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);

            }
            var token = Convert.ToBase64String(randomNumber);
            if(token.Contains('/'))
            {
                token = token.Replace("/", "a");
            }
            return token;
        }

        public async Task<List<Claim>> GetUserClaimsFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims.ToList();
        }

        public async Task<string> GetUserIdFromToken(string token)
        {
            return (await context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token))?.UserId ?? string.Empty;
        }

        public async Task<int> RemoveRefreshToken(string userId)
        {
            throw new NotImplementedException();
        }

        public async Task<int> UpdateRefreshToken(string userId, string refreshToken)
        {
            var data = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken
            };
            var user = await context.RefreshTokens.FirstOrDefaultAsync(r => r.UserId == userId);
            if (user == null)
                return -1;
            user.Token = refreshToken;
            context.RefreshTokens.Update(user);
            return await context.SaveChangesAsync();
        }

        public async Task<bool> ValidateRefreshToken(string refreshToken)
        {
            var user = await context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
            if (user == null)
                return false;
            return true;
        }
    }
}
