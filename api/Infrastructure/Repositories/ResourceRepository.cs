using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;

namespace Nexo.Api.Infrastructure.Repositories;

public class ResourceRepository(NexoDbContext db) : IResourceRepository
{
    public Task<ResourceType?> FindTypeAsync(Guid id) => db.ResourceTypes.SingleOrDefaultAsync(t => t.Id == id);

    public Task<List<ResourceType>> GetTypesAsync() => db.ResourceTypes.OrderBy(t => t.Name).ToListAsync();

    public Task<int> CountByOrganizationAsync(Guid organizationId) =>
        db.Resources.CountAsync(r => r.OrganizationId == organizationId);

    public void Add(Resource resource) => db.Resources.Add(resource);
}
