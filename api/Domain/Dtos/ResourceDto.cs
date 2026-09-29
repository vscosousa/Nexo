using Nexo.Api.Domain.Models;

namespace Nexo.Api.Domain.Dtos;

public class ResourceDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public Guid TypeId { get; set; }

    public string TypeName { get; set; } = "";

    public string? Description { get; set; }

    public ResourceStatus Status { get; set; }

    public Guid OrganizationId { get; set; }
}
