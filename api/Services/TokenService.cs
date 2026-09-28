using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public SessionDto GenerateToken(Account account)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");

        var expires = DateTime.UtcNow.Add(Lifetime);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                ["sub"] = account.Id.ToString(),
                ["orgId"] = account.OrganizationId.ToString(),
                ["role"] = account.Role.ToString(),
            },
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        });
        return new SessionDto { Token = token, ExpiresAt = expires };
    }
}
