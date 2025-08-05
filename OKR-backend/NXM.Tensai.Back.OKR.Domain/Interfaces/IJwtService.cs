using System.Security.Claims;

namespace NXM.Tensai.Back.OKR.Domain;

public interface IJwtService
{
    Task<string> GenerateJwtToken(User user);
    Task<string> GenerateJwtToken(User user, string roleName);
    Task<string> GenerateJwtToken(User user, string roleName, Guid organizationId);
    Task<string> GenerateJwtToken(User user, string roleName, Guid organizationId, Guid? teamId);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
