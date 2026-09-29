namespace Nexo.Api.Domain.Dtos;

/// <summary>What a social login returned for a registration or activation still to be confirmed on the web form.</summary>
public class PendingExternalDto
{
    /// <summary><c>register</c> or <c>activate</c>.</summary>
    public string Intent { get; set; } = "";

    public string Email { get; set; } = "";

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    /// <summary>The organization name typed before leaving for the provider; registration only.</summary>
    public string? OrganizationName { get; set; }
}
