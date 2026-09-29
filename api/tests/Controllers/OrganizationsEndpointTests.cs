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
        PlanId = NexoDbContext.FreePlanId,
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
        Assert.Equal(AccountStatus.Unverified, account.Status);
        Assert.Equal(organization.Id, account.OrganizationId);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<Account>().VerifyHashedPassword(account, account.PasswordHash!, TestData.StrongPassword));
        var email = Assert.Single(factory.Emails.Sent);
        Assert.Equal("ana@example.com", email.To);
        Assert.Contains("/confirm-email?email=ana%40example.com&token=", email.Text);
    }

    [Fact]
    public async Task GivenAnUnconfirmedAdmin_WhenSigningIn_ThenItIsRejected()
    {
        await factory.CreateClient().PostAsJsonAsync("/organizations", Valid);

        var response = await SignInAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GivenTheEmailedLink_WhenConfirming_ThenTheAdminIsActiveAndCanSignIn()
    {
        await factory.CreateClient().PostAsJsonAsync("/organizations", Valid);
        var token = CapturingEmailSender.TokenIn(Assert.Single(factory.Emails.Sent));

        var response = await ConfirmAsync(" Ana@Example.com ", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = NewDbContext();
        var account = await db.Accounts.SingleAsync();
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Null(account.InvitationTokenHash);
        Assert.Equal(HttpStatusCode.NoContent, (await SignInAsync()).StatusCode);
    }

    [Theory]
    [InlineData("ana@example.com", "wrong-token")]
    [InlineData("other@example.com", null)]
    public async Task GivenAWrongEmailOrToken_WhenConfirming_ThenItRejectsAndTheAdminStaysUnconfirmed(string email, string? token)
    {
        await factory.CreateClient().PostAsJsonAsync("/organizations", Valid);
        token ??= CapturingEmailSender.TokenIn(Assert.Single(factory.Emails.Sent));

        var response = await ConfirmAsync(email, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var db = NewDbContext();
        Assert.Equal(AccountStatus.Unverified, (await db.Accounts.SingleAsync()).Status);
    }

    [Fact]
    public async Task GivenAnExpiredLink_WhenConfirming_ThenItRejects()
    {
        await factory.CreateClient().PostAsJsonAsync("/organizations", Valid);
        var token = CapturingEmailSender.TokenIn(Assert.Single(factory.Emails.Sent));
        await ExpireRegistrationAsync();

        var response = await ConfirmAsync("ana@example.com", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GivenAnExpiredUnconfirmedRegistration_WhenRegisteringTheEmailAgain_ThenItReplacesIt()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/organizations", Valid);
        await ExpireRegistrationAsync();

        var response = await client.PostAsJsonAsync("/organizations", new RegisterOrganizationDto
        {
            OrganizationName = "Other Club",
            AdminFirstName = "Ana",
            AdminLastName = "Again",
            AdminEmail = Valid.AdminEmail,
            Password = TestData.StrongPassword,
            PlanId = NexoDbContext.FreePlanId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var db = NewDbContext();
        Assert.Equal("Other Club", (await db.Organizations.SingleAsync()).Name);
        Assert.Equal("Again", (await db.Accounts.SingleAsync()).LastName);
    }

    [Fact]
    public async Task GivenAChosenPlan_WhenRegistering_ThenTheOrganizationIsOnThatPlan()
    {
        var dto = new RegisterOrganizationDto
        {
            OrganizationName = Valid.OrganizationName,
            AdminFirstName = Valid.AdminFirstName,
            AdminLastName = Valid.AdminLastName,
            AdminEmail = Valid.AdminEmail,
            Password = Valid.Password,
            PlanId = NexoDbContext.TeamPlanId,
        };

        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Team", (await response.Content.ReadFromJsonAsync<OrganizationDto>())!.Plan);
        await using var db = NewDbContext();
        Assert.Equal(NexoDbContext.TeamPlanId, (await db.Organizations.SingleAsync()).PlanId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("0f0f0f0f-0000-4000-8000-000000000000")]
    public async Task GivenAMissingOrUnknownPlan_WhenRegistering_ThenItRejectsWithAPlanErrorAndCreatesNothing(string? planId)
    {
        var dto = new RegisterOrganizationDto
        {
            OrganizationName = Valid.OrganizationName,
            AdminFirstName = Valid.AdminFirstName,
            AdminLastName = Valid.AdminLastName,
            AdminEmail = Valid.AdminEmail,
            Password = Valid.Password,
            PlanId = planId is null ? null : Guid.Parse(planId),
        };

        var response = await factory.CreateClient().PostAsJsonAsync("/organizations", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.Equal(["PlanId"], problem!.Errors.Keys.ToArray());
        await AssertNothingCreatedAsync();
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
            PlanId = NexoDbContext.FreePlanId,
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
            PlanId = NexoDbContext.FreePlanId,
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
            PlanId = NexoDbContext.FreePlanId,
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
            PlanId = NexoDbContext.FreePlanId,
        };
        var response = await client.PostAsJsonAsync("/organizations", second);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private Task<HttpResponseMessage> SignInAsync() =>
        factory.CreateClient().PostAsJsonAsync(
            "/auth/sign-in", new SignInDto { Email = Valid.AdminEmail, Password = TestData.StrongPassword });

    private Task<HttpResponseMessage> ConfirmAsync(string email, string token) =>
        factory.CreateClient().PostAsJsonAsync(
            "/accounts/activation/confirm-email", new ConfirmEmailDto { Email = email, Token = token });

    private async Task ExpireRegistrationAsync()
    {
        await using var db = NewDbContext();
        await db.Accounts.ExecuteUpdateAsync(s => s.SetProperty(a => a.InvitationExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
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
