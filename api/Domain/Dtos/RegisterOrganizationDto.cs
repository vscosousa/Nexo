namespace Nexo.Api.Domain.Dtos;

// Properties are nullable so a missing field reaches service validation instead of model binding.
public class RegisterOrganizationDto
{
    public string? OrganizationName { get; set; }

    public string? AdminFirstName { get; set; }

    public string? AdminLastName { get; set; }

    public string? AdminEmail { get; set; }

    public string? Password { get; set; }
}
