using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Services;

public interface ITokenService
{
    /// <summary>Issues a signed session token carrying the account's id, organization, and role.</summary>
    /// <exception cref="InvalidOperationException">The signing key is not configured or is too short.</exception>
    SessionDto GenerateToken(Account account);
}
