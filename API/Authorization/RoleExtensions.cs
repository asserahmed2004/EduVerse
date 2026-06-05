using System.Security.Claims;

namespace API.Authorization
{
    public static class RoleExtensions
    {
        public static bool HasRole(this ClaimsPrincipal user, string role)
        {
            return user.Claims
                .Where(claim => claim.Type == ClaimTypes.Role || claim.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
                .Any(claim => string.Equals(claim.Value, role, StringComparison.OrdinalIgnoreCase));
        }
    }
}
