namespace Nexo.Api.Domain.Models;

/// <summary>A subscription tier and the limits it grants an organization.</summary>
public class Plan
{
    public const string Free = "Free";

    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    /// <summary>Maximum number of active member accounts.</summary>
    public int MemberLimit { get; set; }
}
