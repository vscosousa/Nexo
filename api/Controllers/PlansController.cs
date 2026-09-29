using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PlansController(IPlanService service) : ControllerBase
{
    /// <summary>Returns all plans.</summary>
    [HttpGet]
    public async Task<IEnumerable<PlanDto>> Get()
    {
        return await service.GetAllAsync();
    }
}
