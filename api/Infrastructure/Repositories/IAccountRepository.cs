using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IAccountRepository
{
    /// <summary>Finds the account with the given id, or null if none exists.</summary>
    Task<Account?> GetByIdAsync(Guid id);

    /// <summary>Finds the account registered with the given email, or null if none exists.</summary>
    Task<Account?> FindByEmailAsync(string email);

    /// <summary>Counts the organization's accounts that have the given status.</summary>
    Task<int> CountByOrganizationAndStatusAsync(Guid organizationId, AccountStatus status);

    /// <summary>Tracks the account for insertion; nothing is written until the context is saved.</summary>
    void Add(Account account);
}
