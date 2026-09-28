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
        var plan = await db.Plans.SingleAsync();
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
            new Microsoft.AspNetCore.Identity.PasswordHasher<Account>(),
            db);

        await Assert.ThrowsAsync<ConflictException>(() => service.Register(new RegisterOrganizationDto
        {
            OrganizationName = "Other Club",
            AdminName = "Ana Again",
            AdminEmail = "ana@example.com",
            Password = TestData.StrongPassword,
        }));
    }
}
