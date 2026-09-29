using Nexo.Api.Domain.Models;

namespace Nexo.Api.Domain.Dtos;

public class InviteMemberDto
{
    public string? Email { get; set; }

    /// <summary><c>Member</c> or <c>Staff</c>; required.</summary>
    public Role? Role { get; set; }
}
