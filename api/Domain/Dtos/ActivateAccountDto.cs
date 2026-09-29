namespace Nexo.Api.Domain.Dtos;

public class ActivateAccountDto
{
    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Password { get; set; }

    /// <summary>The link token, read from the activation page's URL and sent automatically (never typed).</summary>
    public string? LinkToken { get; set; }

    /// <summary>The code from the email body, typed in by the person.</summary>
    public string? Code { get; set; }
}
