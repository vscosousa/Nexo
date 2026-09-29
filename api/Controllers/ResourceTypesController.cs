using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Authorize]
[Route("resource-types")]
public class ResourceTypesController(IResourceService service) : ControllerBase
{
    /// <summary>Returns the resource types the caller's organization can use: the system types and its own custom types, by name.</summary>
    /// <response code="200">The types.</response>
    /// <response code="401">The session is missing, invalid, or expired.</response>
    [HttpGet]
    [ProducesResponseType<List<ResourceTypeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<List<ResourceTypeDto>> Get() => service.GetTypes();
}
