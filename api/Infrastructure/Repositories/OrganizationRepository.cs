using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;

namespace Nexo.Api.Infrastructure.Repositories;

public class OrganizationRepository(NexoDbContext db) : IOrganizationRepository
{
    public void Add(Organization organization) => db.Organizations.Add(organization);
}
