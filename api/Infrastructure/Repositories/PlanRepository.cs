using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;

namespace Nexo.Api.Infrastructure.Repositories;

public class PlanRepository(NexoDbContext db) : IPlanRepository
{
    public Task<Plan?> FindByNameAsync(string name) =>
        db.Plans.SingleOrDefaultAsync(p => p.Name == name);
}
