using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IPlanRepository
{
    /// <summary>Finds a plan by its name, or null if none exists.</summary>
    Task<Plan?> FindByNameAsync(string name);
}
