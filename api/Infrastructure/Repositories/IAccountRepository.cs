using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IAccountRepository
{
    /// <summary>Finds the account registered with the given email, or null if none exists.</summary>
    Task<Account?> FindByEmailAsync(string email);

    /// <summary>Tracks the account for insertion; nothing is written until the context is saved.</summary>
    void Add(Account account);
}
