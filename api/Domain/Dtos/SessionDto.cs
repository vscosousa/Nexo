namespace Nexo.Api.Domain.Dtos;

public class SessionDto
{
    public string Token { get; set; } = "";

    public DateTimeOffset ExpiresAt { get; set; }
}
