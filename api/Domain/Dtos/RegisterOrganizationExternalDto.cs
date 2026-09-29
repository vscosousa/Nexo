namespace Nexo.Api.Domain.Dtos;

/// <summary>Registers an organization whose admin signs in with the pending social login instead of a password.</summary>
public class RegisterOrganizationExternalDto
{
    public string? OrganizationName { get; set; }

    public string? AdminFirstName { get; set; }

    public string? AdminLastName { get; set; }

    public Guid? PlanId { get; set; }
}
