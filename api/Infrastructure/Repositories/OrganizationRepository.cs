using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;

namespace Nexo.Api.Infrastructure.Repositories;

public class OrganizationRepository(NexoDbContext db) : IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid id) =>
        db.Organizations.Include(o => o.Plan).SingleOrDefaultAsync(o => o.Id == id);

    public void Add(Organization organization) => db.Organizations.Add(organization);
}
