using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IResourceService
{
    /// <summary>Registers an <c>Available</c> resource in the caller's organization.</summary>
    /// <param name="callerAccountId">The requesting account; null when the caller is unidentified.</param>
    /// <exception cref="Domain.Exceptions.ForbiddenException">The caller is not an active admin or staff account; checked before validation.</exception>
    /// <exception cref="Domain.Exceptions.ValidationException">A field is missing or invalid, or the type is not one the organization can use.</exception>
    /// <exception cref="Domain.Exceptions.ConflictException">The organization's plan resource limit is reached.</exception>
    Task<ResourceDto> Register(Guid? callerAccountId, RegisterResourceDto dto);

    /// <summary>Returns the resource types the caller's organization can use (system and its own custom types), by name.</summary>
    Task<List<ResourceTypeDto>> GetTypes();
}
