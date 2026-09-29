using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    /// <summary>The claim carrying <see cref="Account.SessionVersion"/>; a token whose value is behind the account's is rejected.</summary>
    public const string SessionVersionClaim = "sv";

    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    /// <summary>The <c>iss</c> tokens are issued with and validated against: <c>Jwt:Issuer</c>, or <c>nexo</c>.</summary>
    public static string Issuer(IConfiguration configuration) => configuration["Jwt:Issuer"] is { Length: > 0 } i ? i : "nexo";

    /// <summary>The <c>aud</c> tokens are issued for and validated against: <c>Jwt:Audience</c>, or <c>nexo</c>.</summary>
    public static string Audience(IConfiguration configuration) => configuration["Jwt:Audience"] is { Length: > 0 } a ? a : "nexo";

    public SessionDto GenerateToken(Account account)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");

        var expires = DateTime.UtcNow.Add(Lifetime);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer(configuration),
            Audience = Audience(configuration),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = account.Id.ToString(),
                ["orgId"] = account.OrganizationId.ToString(),
                ["role"] = account.Role.ToString(),
                [SessionVersionClaim] = account.SessionVersion,
            },
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        });
        return new SessionDto { Token = token, ExpiresAt = expires };
    }
}
