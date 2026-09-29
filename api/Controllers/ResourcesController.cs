using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Authorize]
[Route("resources")]
public class ResourcesController(IResourceService service) : ControllerBase
{
    /// <summary>Registers an available resource in the caller's organization.</summary>
    /// <remarks>
    /// The organization is the caller's (the account in the session token's <c>sub</c> claim), never taken from the request.
    /// With the session cookie, the request must also carry the <c>X-XSRF-TOKEN</c> header from <c>GET /auth/csrf</c>.
    /// </remarks>
    /// <response code="201">Created.</response>
    /// <response code="400">A field is missing or invalid, the type is not one the organization can use, or the anti-forgery token is missing or invalid.</response>
    /// <response code="401">The session is missing, invalid, or expired.</response>
    /// <response code="403">The caller is not an admin or staff member; checked before the fields.</response>
    /// <response code="409">The organization's plan resource limit is reached.</response>
    [HttpPost]
    [ProducesResponseType<ResourceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterResourceDto dto)
    {
        try
        {
            var resource = await service.Register(Guid.TryParse(User.FindFirst("sub")?.Value, out var callerId) ? callerId : null, dto);
            return Created($"/resources/{resource.Id}", resource);
        }
        catch (ValidationException e)
        {
            return ValidationProblem(new ValidationProblemDetails(e.Errors));
        }
        catch (ForbiddenException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (ConflictException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
