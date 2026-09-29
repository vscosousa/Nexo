using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;

namespace Nexo.Api.Infrastructure.Repositories;

public class PlanRepository(NexoDbContext db) : IPlanRepository
{
    public Task<Plan?> GetByIdAsync(Guid id) =>
        db.Plans.SingleOrDefaultAsync(p => p.Id == id);

    public Task<List<Plan>> GetAllAsync() =>
        db.Plans.ToListAsync();
}
