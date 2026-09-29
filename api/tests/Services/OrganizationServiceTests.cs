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

public class OrganizationServiceTests(PostgresApiFactory factory)
    : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GivenAnotherRegistrationWinsTheRace_WhenSaving_ThenItConflictsInsteadOfFailing()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
        var plan = await db.Plans.SingleAsync(p => p.Name == Plan.Free);
        var organization = new Organization { Name = "First Club", PlanId = plan.Id };
        db.AddRange(
            organization,
            new Account
            {
                Email = "ana@example.com",
                Role = Role.Admin,
                Status = AccountStatus.Active,
                OrganizationId = organization.Id,
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new OrganizationService(
            new EmailLookupMissesRepository(new AccountRepository(db)),
            new OrganizationRepository(db),
            new PlanRepository(db),
            new ExternalLoginRepository(db),
            new Microsoft.AspNetCore.Identity.PasswordHasher<Account>(),
            factory.Services.GetRequiredService<ITokenService>(),
            factory.Emails,
            factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Nexo.Api.Infrastructure.Email.EmailOptions>>(),
            db);

        await Assert.ThrowsAsync<ConflictException>(() => service.Register(new RegisterOrganizationDto
        {
            OrganizationName = "Other Club",
            AdminFirstName = "Ana",
            AdminLastName = "Again",
            AdminEmail = "ana@example.com",
            Password = TestData.StrongPassword,
            PlanId = NexoDbContext.FreePlanId,
        }));
    }

    [Fact]
    public async Task GivenTheWelcomeEmailCannotBeSent_WhenRegisteringWithGoogle_ThenTheOrganizationIsStillCreated()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoDbContext>();
        var service = new OrganizationService(
            new AccountRepository(db),
            new OrganizationRepository(db),
            new PlanRepository(db),
            new ExternalLoginRepository(db),
            new Microsoft.AspNetCore.Identity.PasswordHasher<Account>(),
            factory.Services.GetRequiredService<ITokenService>(),
            new FailingEmailSender(),
            factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Nexo.Api.Infrastructure.Email.EmailOptions>>(),
            db);

        var (organization, _) = await service.RegisterExternal(
            new RegisterOrganizationExternalDto
            {
                OrganizationName = "Local Club",
                AdminFirstName = "Ana",
                AdminLastName = "Silva",
                PlanId = NexoDbContext.FreePlanId,
            },
            new ExternalIdentity("google", "g-1", "ana@example.com", EmailVerified: true));

        db.ChangeTracker.Clear();
        Assert.True(await db.Organizations.AnyAsync(o => o.Id == organization.Id));
        Assert.True(await db.Accounts.AnyAsync(a => a.Email == "ana@example.com"));
    }

    private sealed class FailingEmailSender : Nexo.Api.Infrastructure.Email.IEmailSender
    {
        public Task SendAsync(Nexo.Api.Infrastructure.Email.EmailMessage message) =>
            throw new InvalidOperationException("SMTP is down.");
    }
}
