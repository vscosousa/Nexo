using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;

namespace Nexo.Api.Infrastructure.Repositories;

public class AccountRepository(NexoDbContext db) : IAccountRepository
{
    public Task<Account?> FindByEmailAsync(string email) =>
        db.Accounts.SingleOrDefaultAsync(a => a.Email == email);

    public void Add(Account account) => db.Accounts.Add(account);
}
