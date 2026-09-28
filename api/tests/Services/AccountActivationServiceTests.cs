using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Domain.Exceptions;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Services;
using Nexo.Api.Tests.Infrastructure;
using Xunit;

namespace Nexo.Api.Tests.Services;

public class AccountActivationServiceTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenAnotherActivationWinsTheRace_WhenSaving_ThenItConflictsInsteadOfOverwriting()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
        var plan = await db.Plans.SingleAsync();
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var pending = new Account
        {
            Email = "bob@example.com",
            Role = Role.Member,
            Status = AccountStatus.Invited,
            OrganizationId = organization.Id,
            InvitationTokenHash = InvitationTokens.Hash(TestData.InvitationToken),
        };
        db.AddRange(organization, pending);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new AccountActivationService(
            new ActivatesAfterLookupRepository(new AccountRepository(db), factory),
            new OrganizationRepository(db),
            new PasswordHasher<Account>(),
            db);

        await Assert.ThrowsAsync<ConflictException>(() => service.Activate(new ActivateAccountDto
        {
            Email = "bob@example.com",
            Name = "Bob",
            Password = TestData.StrongPassword,
            InvitationToken = TestData.InvitationToken,
        }));

        await using var check = factory.Services.CreateAsyncScope();
        var saved = await check.ServiceProvider.GetRequiredService<NexoDbContext>().Accounts.SingleAsync();
        Assert.Equal("Winner", saved.Name);
    }

    /// <summary>Simulates the losing request of a race: the other activation commits after this lookup.</summary>
    private sealed class ActivatesAfterLookupRepository(IAccountRepository inner, PostgresApiFactory factory)
        : IAccountRepository
    {
        public Task<Account?> GetByIdAsync(Guid id) => inner.GetByIdAsync(id);

        public async Task<Account?> FindByEmailAsync(string email)
        {
            var found = await inner.FindByEmailAsync(email);
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<NexoDbContext>().Accounts
                .Where(a => a.Email == email)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Name, "Winner")
                    .SetProperty(a => a.Status, AccountStatus.Active)
                    .SetProperty(a => a.InvitationTokenHash, (string?)null));
            return found;
        }

        public Task<int> CountByOrganizationAndStatusAsync(Guid organizationId, AccountStatus status) =>
            inner.CountByOrganizationAndStatusAsync(organizationId, status);

        public void Add(Account account) => inner.Add(account);
    }
}
