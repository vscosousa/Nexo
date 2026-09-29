using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IOrganizationRepository
{
    /// <summary>Finds the organization with its plan loaded, or null if none exists.</summary>
    Task<Organization?> GetByIdAsync(Guid id);

    /// <summary>
    /// Locks the organization's row until the current transaction ends, so concurrent requests that check
    /// and change its member count run one after another.
    /// </summary>
    Task LockAsync(Guid id);

    /// <summary>Tracks the organization for insertion; nothing is written until the context is saved.</summary>
    void Add(Organization organization);
}
