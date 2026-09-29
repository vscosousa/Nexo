using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Authorize]
[Route("organizations/{organizationId:guid}/invitations")]
public class AccountInvitationsController(IAccountInvitationService service) : ControllerBase
{
    /// <summary>Registers a member's email as a pending account of the organization.</summary>
    /// <remarks>
    /// The one-time invitation token is only delivered by email. If the email cannot be sent, no account is created and the request fails with 500.
    /// The caller is the account in the session token's <c>sub</c> claim (session cookie, or a bearer token).
    /// With the session cookie, the request must also carry the <c>X-XSRF-TOKEN</c> header from <c>GET /auth/csrf</c>.
    /// </remarks>
    /// <response code="201">Created.</response>
    /// <response code="400">The email is missing or invalid, or the anti-forgery token is missing or invalid.</response>
    /// <response code="401">The session is missing, invalid, or expired.</response>
    /// <response code="403">The caller is not an admin of the organization.</response>
    /// <response code="409">The member limit is reached or the email already has an account.</response>
    [HttpPost]
    [ProducesResponseType<AccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invite(
        Guid organizationId, InviteMemberDto dto)
    {
        try
        {
            var account = await service.Invite(organizationId, Guid.TryParse(User.FindFirst("sub")?.Value, out var callerId) ? callerId : null, dto);
            return Created($"/organizations/{organizationId}/invitations/{account.Id}", account);
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
