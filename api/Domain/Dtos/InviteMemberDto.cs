namespace Nexo.Api.Domain.Dtos;

// Nullable so a missing field reaches service validation instead of model binding.
public class InviteMemberDto
{
    public string? Email { get; set; }
}
