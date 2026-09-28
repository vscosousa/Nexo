namespace Nexo.Api.Domain.Dtos;

public class VerifyInvitationDto
{
    public string? Email { get; set; }

    /// <summary>The link token, read from the activation page's URL and sent automatically (never typed).</summary>
    public string? LinkToken { get; set; }

    /// <summary>The code from the email body, typed in by the person.</summary>
    public string? Code { get; set; }
}
