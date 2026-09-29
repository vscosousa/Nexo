namespace Nexo.Api.Domain.Models;

public enum ResourceStatus
{
    Available,
}

/// <summary>An item an organization manages and lends: equipment, a utensil, a vehicle. Spaces are a separate concept.</summary>
public class Resource
{
    public const int NameMaxLength = 200;

    public const int DescriptionMaxLength = 2000;

    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public Guid TypeId { get; set; }

    public string? Description { get; set; }

    public ResourceStatus Status { get; set; } = ResourceStatus.Available;

    public Guid OrganizationId { get; set; }
}
