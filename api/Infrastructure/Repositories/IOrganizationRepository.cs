using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IOrganizationRepository
{
    /// <summary>Tracks the organization for insertion; nothing is written until the context is saved.</summary>
    void Add(Organization organization);
}
