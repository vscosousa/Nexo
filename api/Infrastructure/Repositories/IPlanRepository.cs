using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IPlanRepository
{
    /// <summary>Finds the plan with the given id, or null if none exists.</summary>
    Task<Plan?> GetByIdAsync(Guid id);

    /// <summary>Returns all plans.</summary>
    Task<List<Plan>> GetAllAsync();
}
