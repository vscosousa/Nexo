using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class OrganizationsController(IOrganizationService service) : ControllerBase
{
    /// <summary>Registers an organization and its admin account, which must confirm its email before signing in.</summary>
    /// <remarks>
    /// Responds the same whether or not the email already has an account (its owner is emailed a notice instead), so this
    /// cannot be used to check which emails are registered.
    /// </remarks>
    /// <response code="202">Accepted; a confirmation link (or, for a registered email, a notice) was emailed.</response>
    /// <response code="400">A required field is missing or invalid.</response>
    /// <response code="409">A registration for the same email was saved at the same moment; try again.</response>
    /// <response code="429">Too many requests from this address; the limit is <c>RateLimit:PublicPermitLimit</c> per minute (default 10).</response>
    [HttpPost]
    [EnableRateLimiting("public")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterOrganizationDto dto)
    {
        try
        {
            await service.Register(dto);
            return Accepted();
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
