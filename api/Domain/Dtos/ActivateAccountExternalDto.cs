namespace Nexo.Api.Domain.Dtos;

/// <summary>Activates the invited account with the pending social login instead of a password.</summary>
public class ActivateAccountExternalDto
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
