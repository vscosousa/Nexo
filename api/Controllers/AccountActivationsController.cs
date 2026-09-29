using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

/// <remarks>Every action is limited to <c>RateLimit:PublicPermitLimit</c> requests per minute per address (default 10), answering 429 beyond it.</remarks>
[ApiController]
[Route("accounts/activation")]
[EnableRateLimiting("public")]
public class AccountActivationsController(
    IAccountActivationService service, IAccountInvitationService invitations) : ControllerBase
{
    /// <summary>
    /// Re-sends a fresh invitation code, for a person whose code was lost or who tried too many wrong ones. Only the
    /// code changes; the link token and the invitation's expiry do not, so an already-open activation page keeps
    /// working once the new code is entered.
    /// </summary>
    /// <remarks>Always responds the same way, whether or not the email has a pending invitation, so this cannot be used to check which emails are registered.</remarks>
    /// <response code="202">Accepted; an email was sent if, and only if, an unexpired pending invitation exists for it.</response>
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
    /// <remarks>
    /// A wrong code with the right link token counts as a failed attempt; after <c>InvitationTokens.MaxCodeAttempts</c>
    /// the invitation stops working until a new code is resent.
    /// </remarks>
    /// <response code="200">The email, link token, and code are valid and not yet used.</response>
    /// <response code="403">No pending invitation matches: the email, link token, or code is wrong, expired, or used up; the same response for every cause.</response>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
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
    }

    /// <summary>Activates an invited account with the person's name and password.</summary>
    /// <remarks>Signing the person in is not implemented yet; see the US-003 open decisions.</remarks>
    /// <response code="200">Activated.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="403">No pending invitation matches: the email, link token, or code is wrong, expired, or used up.</response>
    /// <response code="409">The organization's member limit is reached, or the account was activated at the same moment.</response>
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

    /// <summary>Confirms a newly registered admin's email with the token from the emailed link, making the account active.</summary>
    /// <response code="200">Confirmed; the admin can now sign in.</response>
    /// <response code="403">No unconfirmed registration matches the email and token, or the link expired; the same response for every cause.</response>
    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto dto)
    {
        try
        {
            await service.ConfirmEmail(dto.Email ?? "", dto.Token ?? "");
            return Ok();
        }
        catch (ForbiddenException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status403Forbidden);
        }
    }
}
