using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("organizations/{organizationId:guid}/invitations")]
public class AccountInvitationsController(IAccountInvitationService service) : ControllerBase
{
    /// <summary>Registers a member's email as a pending account of the organization.</summary>
    /// <remarks>
    /// The caller is read from the temporary <c>X-Account-Id</c> header until JWT authentication (US-005) exists.
    /// </remarks>
    /// <response code="201">Created.</response>
    /// <response code="400">The email is missing or invalid.</response>
    /// <response code="403">The caller is not an admin of the organization.</response>
    /// <response code="409">The member limit is reached or the email already has an account.</response>
    [HttpPost]
    [ProducesResponseType<AccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invite(
        Guid organizationId, InviteMemberDto dto, [FromHeader(Name = "X-Account-Id")] Guid? callerAccountId)
    {
        try
        {
            var account = await service.Invite(organizationId, callerAccountId, dto);
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
