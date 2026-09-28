namespace Nexo.Api.Domain.Models;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public Guid PlanId { get; set; }

    public Plan? Plan { get; set; }
}
