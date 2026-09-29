using Nexo.Api.Domain.Dtos;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Mappers;

namespace Nexo.Api.Services;

public class PlanService(IPlanRepository repository) : IPlanService
{
    public async Task<IEnumerable<PlanDto>> GetAllAsync()
    {
        var plans = await repository.GetAllAsync();
        return plans.Select(PlanMapper.ToDto);
    }
}
