using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Repositories;

namespace Nexo.Api.Tests.Services;

/// <summary>Simulates the losing request of a race: the email lookup runs before the winner's row is visible.</summary>
internal sealed class EmailLookupMissesRepository(IAccountRepository inner) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id) => inner.GetByIdAsync(id);

    public Task<Account?> FindByEmailAsync(string email) => Task.FromResult<Account?>(null);

    public Task<int> CountByOrganizationAndStatusAsync(Guid organizationId, AccountStatus status) =>
        inner.CountByOrganizationAndStatusAsync(organizationId, status);

    public void Add(Account account) => inner.Add(account);
}
