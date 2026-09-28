using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nexo.Api.Infrastructure.Email;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Services;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Services;

public class AccountInvitationServiceTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenAnotherInvitationWinsTheRace_WhenSaving_ThenItConflictsInsteadOfFailing()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
        var plan = await db.Plans.SingleAsync();
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var admin = new Account
        {
            Email = "ana@example.com",
            Role = Role.Admin,
            Status = AccountStatus.Active,
            OrganizationId = organization.Id,
        };
        var winner = new Account
        {
            Email = "bob@example.com",
            Role = Role.Member,
            Status = AccountStatus.Invited,
            OrganizationId = organization.Id,
        };
        db.AddRange(organization, admin, winner);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = NewService(new EmailLookupMissesRepository(new AccountRepository(db)), db, new CapturingEmailSender());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.Invite(organization.Id, admin.Id, new InviteMemberDto { Email = "bob@example.com" }));
    }

    [Fact]
    public async Task GivenTheEmailCannotBeSent_WhenInviting_ThenNoInvitedAccountIsLeftBehind()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
        var plan = await db.Plans.SingleAsync();
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var admin = new Account
        {
            Email = "ana@example.com",
            Role = Role.Admin,
            Status = AccountStatus.Active,
            OrganizationId = organization.Id,
        };
        db.AddRange(organization, admin);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = NewService(new AccountRepository(db), db, new FailingEmailSender());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.Invite(organization.Id, admin.Id, new InviteMemberDto { Email = "bob@example.com" }));

        await using var check = factory.Services.CreateAsyncScope();
        Assert.Equal(1, await check.ServiceProvider.GetRequiredService<NexoDbContext>().Accounts.CountAsync());
    }

    private static AccountInvitationService NewService(IAccountRepository accounts, NexoDbContext db, IEmailSender email) =>
        new(accounts, new OrganizationRepository(db), email, Options.Create(new EmailOptions()), db);

    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message) => throw new InvalidOperationException("SMTP is down.");
    }
}
