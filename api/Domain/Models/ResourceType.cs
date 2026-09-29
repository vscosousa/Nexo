namespace Nexo.Api.Domain.Models;

/// <summary>A kind of resource: a system type shared by every organization, or one organization's custom type (ADR-012).</summary>
public class ResourceType
{
    public const int NameMaxLength = 50;

    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>Null for a system type; the owning organization for a custom type.</summary>
    public Guid? OrganizationId { get; set; }
}
