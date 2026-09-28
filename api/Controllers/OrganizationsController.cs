using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class OrganizationsController(IOrganizationService service) : ControllerBase
{
    /// <summary>Registers an organization and its admin account.</summary>
    /// <response code="201">Created.</response>
    /// <response code="400">A required field is missing or invalid.</response>
    /// <response code="409">The admin email already has an account.</response>
    [HttpPost]
    [ProducesResponseType<OrganizationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterOrganizationDto dto)
    {
        try
        {
            var organization = await service.Register(dto);
            return Created($"/organizations/{organization.Id}", organization);
        }
        catch (ValidationException e)
        {
            return ValidationProblem(new ValidationProblemDetails(e.Errors));
        }
        catch (ConflictException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
