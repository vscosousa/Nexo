using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Services;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>US-004 acceptance tests, run against PostgreSQL.</summary>
public class ResourcesEndpointTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenAnAdminAndValidDetails_WhenRegistering_ThenTheResourceIsAvailableInTheirOrganization()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();

        var response = await RegisterAsync(adminId, new RegisterResourceDto
        {
            Name = "  Pressure washer ",
            TypeId = NexoDbContext.EquipmentTypeId,
            Description = "Kärcher K5",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ResourceDto>();
        Assert.NotNull(dto);
        Assert.Equal("Pressure washer", dto.Name);
        Assert.Equal(NexoDbContext.EquipmentTypeId, dto.TypeId);
        Assert.Equal("Equipment", dto.TypeName);
        Assert.Equal("Kärcher K5", dto.Description);
        Assert.Equal(ResourceStatus.Available, dto.Status);
        Assert.Equal(organizationId, dto.OrganizationId);
        Assert.Equal($"/resources/{dto.Id}", response.Headers.Location?.OriginalString);

        await using var db = NewDbContext();
        var resource = await db.Resources.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(dto.Id, resource.Id);
        Assert.Equal(organizationId, resource.OrganizationId);
        Assert.Equal(ResourceStatus.Available, resource.Status);
    }

    [Fact]
    public async Task GivenAStaffCaller_WhenRegistering_ThenTheResourceIsCreated()
    {
        var (organizationId, _) = await SeedOrganizationAsync();
        var staffId = await AddAccountAsync(organizationId, "sam@example.com", Role.Staff);

        var response = await RegisterAsync(staffId, Valid());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GivenABlankDescription_WhenRegistering_ThenItIsStoredAsNull()
    {
        var (_, adminId) = await SeedOrganizationAsync();

        var response = await RegisterAsync(adminId, new RegisterResourceDto { Name = "Van", TypeId = NexoDbContext.VehicleTypeId, Description = "  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var db = NewDbContext();
        Assert.Null((await db.Resources.IgnoreQueryFilters().SingleAsync()).Description);
    }

    [Fact]
    public async Task GivenAMemberCaller_WhenRegisteringInvalidDetails_ThenItRejectsWithForbiddenBeforeValidating()
    {
        var (organizationId, _) = await SeedOrganizationAsync();
        var memberId = await AddAccountAsync(organizationId, "bob@example.com", Role.Member);

        var response = await RegisterAsync(memberId, new RegisterResourceDto { Name = "" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertResourceCountAsync(0);
    }

    [Fact]
    public async Task GivenAStaffAccountThatIsNotActive_WhenRegistering_ThenItRejectsWithForbidden()
    {
        var (organizationId, _) = await SeedOrganizationAsync();
        var staffId = await AddAccountAsync(organizationId, "sam@example.com", Role.Staff, AccountStatus.Invited);

        var response = await RegisterAsync(staffId, Valid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertResourceCountAsync(0);
    }

    [Theory]
    [InlineData("", "UTENSIL", null)]
    [InlineData("TOO_LONG", "UTENSIL", null)]
    [InlineData("Pan", "UTENSIL", "TOO_LONG")]
    [InlineData("Pan", null, null)]
    [InlineData("Pan", "UNKNOWN", null)]
    public async Task GivenMissingOrInvalidDetails_WhenRegistering_ThenItRejectsAndCreatesNothing(
        string name, string? type, string? description)
    {
        var (_, adminId) = await SeedOrganizationAsync();

        var response = await RegisterAsync(adminId, new RegisterResourceDto
        {
            Name = name == "TOO_LONG" ? new string('a', Resource.NameMaxLength + 1) : name,
            TypeId = type switch { "UTENSIL" => NexoDbContext.UtensilTypeId, "UNKNOWN" => Guid.NewGuid(), _ => null },
            Description = description == "TOO_LONG" ? new string('a', Resource.DescriptionMaxLength + 1) : description,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertResourceCountAsync(0);
    }

    [Fact]
    public async Task GivenACustomTypeOfAnotherOrganization_WhenRegistering_ThenItRejectsAsAnUnknownType()
    {
        var (_, adminId) = await SeedOrganizationAsync();
        var (otherOrganizationId, _) = await SeedOrganizationAsync("Other Club", "other@example.com");
        var foreignTypeId = await AddCustomTypeAsync(otherOrganizationId, "Kayak");

        var response = await RegisterAsync(adminId, new RegisterResourceDto { Name = "Kayak 1", TypeId = foreignTypeId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertResourceCountAsync(0);
    }

    [Fact]
    public async Task GivenACustomTypeOfTheOrganization_WhenRegistering_ThenTheResourceUsesIt()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        var typeId = await AddCustomTypeAsync(organizationId, "Kayak");

        var response = await RegisterAsync(adminId, new RegisterResourceDto { Name = "Kayak 1", TypeId = typeId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Kayak", (await response.Content.ReadFromJsonAsync<ResourceDto>())!.TypeName);
    }

    [Fact]
    public async Task GivenTheFreePlanResourceLimitIsReached_WhenRegistering_ThenItRejectsWithConflict()
    {
        var (organizationId, adminId) = await SeedOrganizationAsync();
        await AddResourcesAsync(organizationId, 10);

        var response = await RegisterAsync(adminId, Valid());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertResourceCountAsync(10);
    }

    [Fact]
    public async Task GivenAnotherOrganizationAtItsLimit_WhenRegistering_ThenItsResourcesDoNotCount()
    {
        var (_, adminId) = await SeedOrganizationAsync();
        var (otherOrganizationId, _) = await SeedOrganizationAsync("Other Club", "other@example.com");
        await AddResourcesAsync(otherOrganizationId, 10);

        var response = await RegisterAsync(adminId, Valid());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GivenNoSession_WhenRegistering_ThenItRejectsWithUnauthorized()
    {
        await SeedOrganizationAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/resources", Valid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertResourceCountAsync(0);
    }

    [Fact]
    public async Task GivenASignedInMember_WhenListingTypes_ThenItGetsTheSystemTypesAndOnlyItsOrganizationsCustomOnes()
    {
        var (organizationId, _) = await SeedOrganizationAsync();
        var memberId = await AddAccountAsync(organizationId, "bob@example.com", Role.Member);
        var (otherOrganizationId, _) = await SeedOrganizationAsync("Other Club", "other@example.com");
        await AddCustomTypeAsync(organizationId, "Kayak");
        await AddCustomTypeAsync(otherOrganizationId, "Tractor");

        var response = await SendAsync(memberId, new HttpRequestMessage(HttpMethod.Get, "/resource-types"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var types = await response.Content.ReadFromJsonAsync<List<ResourceTypeDto>>();
        Assert.Equal(["Equipment", "Kayak", "Other", "Utensil", "Vehicle"], types!.Select(t => t.Name));
        Assert.Contains(types!, t => t.Id == NexoDbContext.UtensilTypeId);
    }

    [Fact]
    public async Task GivenNoSession_WhenListingTypes_ThenItRejectsWithUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/resource-types");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static RegisterResourceDto Valid() => new() { Name = "Projector", TypeId = NexoDbContext.EquipmentTypeId };

    private async Task<(Guid OrganizationId, Guid AdminId)> SeedOrganizationAsync(
        string name = "Local Club", string adminEmail = "ana@example.com")
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", new RegisterOrganizationDto
        {
            OrganizationName = name,
            AdminFirstName = "Ana",
            AdminLastName = "Admin",
            AdminEmail = adminEmail,
            Password = TestData.StrongPassword,
            PlanId = NexoDbContext.FreePlanId,
        });
        response.EnsureSuccessStatusCode();
        await using var db = NewDbContext();
        await db.Accounts.Where(a => a.Email == adminEmail)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AccountStatus.Active));
        var admin = await db.Accounts.SingleAsync(a => a.Email == adminEmail);
        return (admin.OrganizationId, admin.Id);
    }

    private async Task<Guid> AddAccountAsync(
        Guid organizationId, string email, Role role, AccountStatus status = AccountStatus.Active)
    {
        await using var db = NewDbContext();
        var account = new Account { Email = email, Role = role, Status = status, OrganizationId = organizationId };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private async Task<Guid> AddCustomTypeAsync(Guid organizationId, string name)
    {
        await using var db = NewDbContext();
        var type = new ResourceType { Name = name, OrganizationId = organizationId };
        db.ResourceTypes.Add(type);
        await db.SaveChangesAsync();
        return type.Id;
    }

    private async Task AddResourcesAsync(Guid organizationId, int count)
    {
        await using var db = NewDbContext();
        for (var i = 0; i < count; i++)
            db.Resources.Add(new Resource { Name = $"Chair {i}", TypeId = NexoDbContext.OtherTypeId, OrganizationId = organizationId });
        await db.SaveChangesAsync();
    }

    private Task<HttpResponseMessage> RegisterAsync(Guid callerId, RegisterResourceDto dto) =>
        SendAsync(callerId, new HttpRequestMessage(HttpMethod.Post, "/resources") { Content = JsonContent.Create(dto) });

    private async Task<HttpResponseMessage> SendAsync(Guid callerId, HttpRequestMessage request)
    {
        await using var db = NewDbContext();
        var caller = await db.Accounts.SingleAsync(a => a.Id == callerId);
        request.Headers.Authorization = new("Bearer", factory.Services.GetRequiredService<ITokenService>().GenerateToken(caller).Token);
        return await factory.CreateClient().SendAsync(request);
    }

    private async Task AssertResourceCountAsync(int expected)
    {
        await using var db = NewDbContext();
        Assert.Equal(expected, await db.Resources.IgnoreQueryFilters().CountAsync());
    }

    private NexoDbContext NewDbContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<NexoDbContext>();
}
