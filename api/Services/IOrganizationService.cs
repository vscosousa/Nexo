using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IOrganizationService
{
    /// <summary>Creates an organization on the Free plan together with its admin account.</summary>
    /// <exception cref="Domain.Exceptions.ValidationException">A required field is missing or the email is malformed.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The email already has an account.</exception>
    Task<OrganizationDto> Register(RegisterOrganizationDto dto);
}
