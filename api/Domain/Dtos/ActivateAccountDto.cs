namespace Nexo.Api.Domain.Dtos;

// Nullable so a missing field reaches service validation instead of model binding.
public class ActivateAccountDto
{
    public string? Email { get; set; }

    public string? Name { get; set; }

    public string? Password { get; set; }

    public string? InvitationToken { get; set; }
}
