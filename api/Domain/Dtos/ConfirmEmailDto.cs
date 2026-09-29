namespace Nexo.Api.Domain.Dtos;

public class ConfirmEmailDto
{
    public string? Email { get; set; }

    /// <summary>The token from the confirmation link's URL.</summary>
    public string? Token { get; set; }
}
