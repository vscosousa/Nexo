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
        var plan = await db.Plans.SingleAsync(p => p.Name == Plan.Free);
        var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
        var pending = new Account
        {
            Email = "bob@example.com",
            Role = Role.Member,
            Status = AccountStatus.Invited,
            OrganizationId = organization.Id,
            InvitationTokenHash = InvitationTokens.HashToken(TestData.InvitationLinkToken),
            InvitationCodeHash = InvitationTokens.HashCode(TestData.InvitationCode),
            InvitationExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        db.AddRange(organization, pending);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new AccountActivationService(
            new ActivatesAfterLookupRepository(new AccountRepository(db), factory),
            new OrganizationRepository(db),
            new ExternalLoginRepository(db),
            new PasswordHasher<Account>(),
            factory.Services.GetRequiredService<ITokenService>(),
            db);

        await Assert.ThrowsAsync<ConflictException>(() => service.Activate(new ActivateAccountDto
        {
            Email = "bob@example.com",
            FirstName = "Bob",
            LastName = "Builder",
            Password = TestData.StrongPassword,
            LinkToken = TestData.InvitationLinkToken,
            Code = TestData.InvitationCode,
        }));

        await using var check = factory.Services.CreateAsyncScope();
        var saved = await check.ServiceProvider.GetRequiredService<NexoDbContext>().Accounts.SingleAsync();
        Assert.Equal("Winner", saved.FirstName);
    }

    [Fact]
    public async Task GivenTwoActivationsRaceForTheLastSeat_WhenActivating_ThenOnlyOneSucceeds()
    {
        await using (var setup = factory.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<NexoDbContext>();
            var plan = await db.Plans.SingleAsync(p => p.Name == Plan.Free);
            var organization = new Organization { Name = "Local Club", PlanId = plan.Id };
            db.Add(organization);
            db.AddRange(Enumerable.Range(0, plan.MemberLimit - 1).Select(i => new Account
            {
                Email = $"member{i}@example.com",
                Role = i == 0 ? Role.Admin : Role.Member,
                Status = AccountStatus.Active,
                OrganizationId = organization.Id,
            }));
            db.AddRange(new[] { "bob@example.com", "eva@example.com" }.Select(email => new Account
            {
                Email = email,
                Role = Role.Member,
                Status = AccountStatus.Invited,
                OrganizationId = organization.Id,
                InvitationTokenHash = InvitationTokens.HashToken(TestData.InvitationLinkToken),
                InvitationCodeHash = InvitationTokens.HashCode(TestData.InvitationCode),
                InvitationExpiresAt = DateTime.UtcNow.AddDays(1),
            }));
            await db.SaveChangesAsync();
        }

        var lookedUp = new RaceGate(2, Timeout.InfiniteTimeSpan);
        var counted = new RaceGate(2, TimeSpan.FromSeconds(1));
        async Task<bool> Activate(string email)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
            var service = new AccountActivationService(
                new RacingRepository(new AccountRepository(db), lookedUp, counted),
                new OrganizationRepository(db),
                new ExternalLoginRepository(db),
                new PasswordHasher<Account>(),
                factory.Services.GetRequiredService<ITokenService>(),
                db);
            try
            {
                await service.Activate(new ActivateAccountDto
                {
                    Email = email,
                    FirstName = "New",
                    LastName = "Member",
                    Password = TestData.StrongPassword,
                    LinkToken = TestData.InvitationLinkToken,
                    Code = TestData.InvitationCode,
                });
                return true;
            }
            catch (ConflictException e)
            {
                Console.Error.WriteLine("DBG " + e.Message);
                return false;
            }
        }

        var results = await Task.WhenAll(Activate("bob@example.com"), Activate("eva@example.com"));

        Assert.Single(results, succeeded => succeeded);
    }

    /// <summary>Holds each racer until every racer has arrived, or until the timeout passes because one is blocked.</summary>
    private sealed class RaceGate(int racers, TimeSpan timeout)
    {
        private int _counted;
        private readonly TaskCompletionSource _allCounted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Arrive()
        {
            if (Interlocked.Increment(ref _counted) == racers)
                _allCounted.TrySetResult();
            return Task.WhenAny(_allCounted.Task, Task.Delay(timeout));
        }
    }

    /// <summary>Lines both racers up after the account lookup, then again after the member count, so both counts happen before either saves.</summary>
    private sealed class RacingRepository(IAccountRepository inner, RaceGate lookedUp, RaceGate counted) : IAccountRepository
    {
        public Task<Account?> GetByIdAsync(Guid id) => inner.GetByIdAsync(id);

        public async Task<Account?> FindByEmailAsync(string email)
        {
            var found = await inner.FindByEmailAsync(email);
            await lookedUp.Arrive();
            return found;
        }

        public async Task<int> CountByOrganizationAndStatusAsync(Guid organizationId, AccountStatus status)
        {
            var count = await inner.CountByOrganizationAndStatusAsync(organizationId, status);
            await counted.Arrive();
            return count;
        }

        public void Add(Account account) => inner.Add(account);
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
                    .SetProperty(a => a.FirstName, "Winner")
                    .SetProperty(a => a.Status, AccountStatus.Active)
                    .SetProperty(a => a.InvitationTokenHash, (string?)null));
            return found;
        }

        public Task<int> CountByOrganizationAndStatusAsync(Guid organizationId, AccountStatus status) =>
            inner.CountByOrganizationAndStatusAsync(organizationId, status);

        public void Add(Account account) => inner.Add(account);
    }
}
