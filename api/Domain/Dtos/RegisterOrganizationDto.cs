namespace Nexo.Api.Domain.Dtos;

public class RegisterOrganizationDto
{
    public string? OrganizationName { get; set; }

    public string? AdminFirstName { get; set; }

    public string? AdminLastName { get; set; }

    public string? AdminEmail { get; set; }

    public string? Password { get; set; }

    /// <summary>The plan chosen before registering; must match one returned by <c>GET /plans</c>.</summary>
    public Guid? PlanId { get; set; }
}
