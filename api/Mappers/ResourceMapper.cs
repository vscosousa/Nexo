using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;

namespace Nexo.Api.Mappers;

public static class ResourceMapper
{
    public static Resource ToResource(RegisterResourceDto dto, Guid organizationId) => new()
    {
        Name = dto.Name!.Trim(),
        TypeId = dto.TypeId!.Value,
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        Status = ResourceStatus.Available,
        OrganizationId = organizationId,
    };

    public static ResourceTypeDto ToDto(ResourceType type) => new() { Id = type.Id, Name = type.Name };

    public static ResourceDto ToDto(Resource resource, ResourceType type) => new()
    {
        Id = resource.Id,
        Name = resource.Name,
        TypeId = resource.TypeId,
        TypeName = type.Name,
        Description = resource.Description,
        Status = resource.Status,
        OrganizationId = resource.OrganizationId,
    };
}
