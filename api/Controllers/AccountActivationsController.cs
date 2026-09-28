using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("accounts/activation")]
public class AccountActivationsController(IAccountActivationService service) : ControllerBase
{
    /// <summary>Activates an invited account with the person's name and password.</summary>
    /// <remarks>Signing the person in is not implemented yet; see the US-003 open decisions.</remarks>
    /// <response code="200">Activated.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="403">No account is registered for the email, or the invitation token is wrong.</response>
    /// <response code="409">The account is already active or the organization's member limit is reached.</response>
    [HttpPost]
    [ProducesResponseType<AccountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(ActivateAccountDto dto)
    {
        try
        {
            return Ok(await service.Activate(dto));
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
