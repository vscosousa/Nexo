using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IPlanService
{
    /// <summary>Gets all plans.</summary>
    Task<IEnumerable<PlanDto>> GetAllAsync();
}
