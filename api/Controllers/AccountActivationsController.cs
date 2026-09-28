using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("accounts/activation")]
public class AccountActivationsController(
    IAccountActivationService service, IAccountInvitationService invitations) : ControllerBase
{
    /// <summary>
    /// Re-sends a fresh invitation code, for a person whose code expired or was lost. Only the code changes;
    /// the link token does not, so an already-open activation page keeps working once the new code is entered.
    /// </summary>
    /// <remarks>Always responds the same way, whether or not the email has a pending invitation, so this cannot be used to check which emails are registered.</remarks>
    /// <response code="202">Accepted; an email was sent if, and only if, a pending invitation exists for it.</response>
    [HttpPost("resend")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Resend(ResendInvitationDto dto)
    {
        await invitations.Resend(dto.Email ?? "");
        return Accepted();
    }

    /// <summary>
    /// Checks the email, link token, and invitation code without activating anything, so the frontend can
    /// hide the account form until the code is confirmed correct.
    /// </summary>
    /// <response code="200">The email, link token, and code are valid and not yet used.</response>
    /// <response code="403">No account is registered for the email, or the link token or code is wrong or expired.</response>
    /// <response code="409">The account is already active.</response>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Verify(VerifyInvitationDto dto)
    {
        try
        {
            await service.VerifyInvitation(dto.Email ?? "", dto.LinkToken ?? "", dto.Code ?? "");
            return Ok();
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

    /// <summary>Activates an invited account with the person's name and password.</summary>
    /// <remarks>Signing the person in is not implemented yet; see the US-003 open decisions.</remarks>
    /// <response code="200">Activated.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="403">No account is registered for the email, or the link token or code is wrong or expired.</response>
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
