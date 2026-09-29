namespace Nexo.Api.Domain.Dtos;

/// <summary>The anti-forgery request token the web app sends back in the <c>X-XSRF-TOKEN</c> header on changes.</summary>
public class AntiforgeryTokenDto
{
    public string Token { get; set; } = "";
}
