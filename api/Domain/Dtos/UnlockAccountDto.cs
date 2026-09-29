namespace Nexo.Api.Domain.Dtos;

public class UnlockAccountDto
{
    public string? Email { get; set; }

    /// <summary>The token from the unlock link's URL.</summary>
    public string? Token { get; set; }
}
