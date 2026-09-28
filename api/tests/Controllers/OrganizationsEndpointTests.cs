using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

/// <summary>US-001 acceptance tests, run against PostgreSQL.</summary>
public class OrganizationsEndpointTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    private static readonly RegisterOrganizationDto Valid = new()
    {
        OrganizationName = "Local Club",
        AdminFirstName = "Ana",
        AdminLastName = "Admin",
        AdminEmail = "ana@example.com",
        Password = TestData.StrongPassword,
    };

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenValidDetails_WhenRegistering_ThenItCreatesTheOrganizationAndItsAdminAccount()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", Valid);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.NotNull(dto);
        Assert.Equal("Local Club", dto.Name);
        Assert.Equal("Free", dto.Plan);

        await using var db = NewDbContext();
        var organization = await db.Organizations.SingleAsync();
        Assert.Equal(dto.Id, organization.Id);
        var plan = await db.Plans.SingleAsync(p => p.Id == organization.PlanId);
        Assert.Equal("Free", plan.Name);
        Assert.Equal(20, plan.MemberLimit);
        var account = await db.Accounts.SingleAsync();
        Assert.Equal("ana@example.com", account.Email);
        Assert.Equal(Role.Admin, account.Role);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Equal(organization.Id, account.OrganizationId);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<Account>().VerifyHashedPassword(account, account.PasswordHash!, TestData.StrongPassword));
    }

    [Theory]
    [InlineData("", "Ana", "Admin", "ana@example.com")]
    [InlineData("Local Club", "", "Admin", "ana@example.com")]
    [InlineData("Local Club", "Ana", "", "ana@example.com")]
    [InlineData("Local Club", "Ana", "Admin", "")]
    [InlineData("Local Club", "Ana", "Admin", "not-an-email")]
    [InlineData("Local Club", "Ana", "Admin", "ana@example.com", "")]
    [InlineData("Local Club", "Ana", "Admin", "ana@example.com", "weak")]
    [InlineData("Local Club", "Ana", "Admin", "ana@example.com", "Xx!LocalClub9")]
    [InlineData("Local Club", "Ana", "Admin", "ana@example.com", "Xx!L0c@l Cl_ub9")]
    [InlineData("Local Club", "Ana", "Admin", "ana@example.com", "Xx!Adm1n-9zq")]
    [InlineData("Local Club", "Ana", "Admin", "ana@example.com", "Xx!ANA9zqvw")]
    public async Task GivenMissingOrInvalidFields_WhenRegistering_ThenItRejectsWithValidationErrorsAndCreatesNothing(
        string organizationName, string adminFirstName, string adminLastName, string adminEmail,
        string password = TestData.StrongPassword)
    {
        var dto = new RegisterOrganizationDto
        {
            OrganizationName = organizationName,
            AdminFirstName = adminFirstName,
            AdminLastName = adminLastName,
            AdminEmail = adminEmail,
            Password = password,
        };

        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task GivenOverLongFields_WhenRegistering_ThenItRejectsWithValidationErrorsAndCreatesNothing()
    {
        var dto = new RegisterOrganizationDto
        {
            OrganizationName = new string('o', 201),
            AdminFirstName = new string('n', 101),
            AdminLastName = new string('n', 101),
            AdminEmail = new string('e', 310) + "@example.com",
            Password = TestData.StrongPassword,
        };

        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.Equal(
            ["AdminEmail", "AdminFirstName", "AdminLastName", "OrganizationName"],
            problem!.Errors.Keys.Order().ToArray());
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task GivenAnEmailAlreadyInUse_WhenRegistering_ThenItRejectsWithConflictAndCreatesNothingMore()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/organizations", Valid);

        var second = new RegisterOrganizationDto
        {
            OrganizationName = "Other Club",
            AdminFirstName = "Ana",
            AdminLastName = "Again",
            AdminEmail = Valid.AdminEmail,
            Password = TestData.StrongPassword,
        };
        var response = await client.PostAsJsonAsync("/organizations", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var db = NewDbContext();
        Assert.Equal(1, await db.Organizations.CountAsync());
        Assert.Equal(1, await db.Accounts.CountAsync());
    }

    [Fact]
    public async Task GivenTheSameEmailInADifferentCase_WhenRegistering_ThenItIsTreatedAsTheSameAccount()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/organizations", Valid);

        var second = new RegisterOrganizationDto
        {
            OrganizationName = "Other Club",
            AdminFirstName = "Ana",
            AdminLastName = "Again",
            AdminEmail = " ANA@Example.com ",
            Password = TestData.StrongPassword,
        };
        var response = await client.PostAsJsonAsync("/organizations", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task AssertNothingCreatedAsync()
    {
        await using var db = NewDbContext();
        Assert.Empty(await db.Organizations.ToListAsync());
        Assert.Empty(await db.Accounts.ToListAsync());
    }

    private NexoDbContext NewDbContext() =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<NexoDbContext>();
}
