using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IResourceRepository
{
    /// <summary>Finds a resource type the caller's organization can use (a system type or its own custom type), or null.</summary>
    Task<ResourceType?> FindTypeAsync(Guid id);

    /// <summary>Returns the resource types the caller's organization can use, ordered by name.</summary>
    Task<List<ResourceType>> GetTypesAsync();

    /// <summary>Counts the organization's resources.</summary>
    Task<int> CountByOrganizationAsync(Guid organizationId);

    /// <summary>Tracks the resource for insertion; nothing is written until the context is saved.</summary>
    void Add(Resource resource);
}
