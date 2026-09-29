using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IOrganizationService
{
    /// <summary>Creates an organization on the chosen plan together with its admin account.</summary>
    /// <exception cref="Domain.Exceptions.ValidationException">A required field is missing, the email is malformed, or the plan does not exist.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The email already has an account.</exception>
    Task<OrganizationDto> Register(RegisterOrganizationDto dto);

    /// <summary>
    /// Creates an organization whose admin has no password and signs in with the given social login, which is linked
    /// to the new account; the admin's email is the provider's verified email. Returns the admin's session.
    /// </summary>
    /// <exception cref="Domain.Exceptions.ValidationException">A required field is missing or the plan does not exist.</exception>
    /// <exception cref="Domain.Exceptions.ForbiddenException">The provider did not attest a verified email.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The email, or the social login, already belongs to an account.</exception>
    Task<(OrganizationDto Organization, SessionDto Session)> RegisterExternal(
        RegisterOrganizationExternalDto dto, ExternalIdentity identity);
}
