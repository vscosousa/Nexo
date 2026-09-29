using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Mappers;

namespace Nexo.Api.Services;

public class ResourceService(
    IAccountRepository accounts,
    IOrganizationRepository organizations,
    IResourceRepository resources,
    NexoDbContext db) : IResourceService
{
    public async Task<ResourceDto> Register(Guid? callerAccountId, RegisterResourceDto dto)
    {
        var caller = callerAccountId is { } id ? await accounts.GetByIdAsync(id) : null;
        if (caller is not { Status: AccountStatus.Active, Role: Role.Admin or Role.Staff })
            throw new ForbiddenException("Only an admin or staff member can register resources.");

        var errors = new Dictionary<string, string[]>();
        FieldRules.Text(errors, nameof(dto.Name), "Name", dto.Name, Resource.NameMaxLength);
        if (dto.Description?.Trim().Length > Resource.DescriptionMaxLength)
            errors[nameof(dto.Description)] = [$"Description must be at most {Resource.DescriptionMaxLength} characters."];
        var type = dto.TypeId is { } typeId ? await resources.FindTypeAsync(typeId) : null;
        if (type is null)
            errors[nameof(dto.TypeId)] = ["A known resource type is required."];
        if (errors.Count > 0)
            throw new Domain.Exceptions.ValidationException(errors);

        await using var transaction = await db.Database.BeginTransactionAsync();
        await organizations.LockAsync(caller.OrganizationId);
        var organization = await organizations.GetByIdAsync(caller.OrganizationId)
            ?? throw new InvalidOperationException("The caller's organization does not exist.");
        if (await resources.CountByOrganizationAsync(organization.Id) >= organization.Plan!.ResourceLimit)
            throw new ConflictException("The organization's resource limit has been reached.");

        var resource = ResourceMapper.ToResource(dto, organization.Id);
        resources.Add(resource);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return ResourceMapper.ToDto(resource, type!);
    }

    public async Task<List<ResourceTypeDto>> GetTypes() =>
        [.. (await resources.GetTypesAsync()).Select(ResourceMapper.ToDto)];
}
