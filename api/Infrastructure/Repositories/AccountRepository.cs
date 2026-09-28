using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;

namespace Nexo.Api.Infrastructure.Repositories;

public class AccountRepository(NexoDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id) => db.Accounts.SingleOrDefaultAsync(a => a.Id == id);

    public Task<Account?> FindByEmailAsync(string email) =>
        db.Accounts.SingleOrDefaultAsync(a => a.Email == email);

    public Task<int> CountByOrganizationAndStatusAsync(Guid organizationId, AccountStatus status) =>
        db.Accounts.CountAsync(a => a.OrganizationId == organizationId && a.Status == status);

    public void Add(Account account) => db.Accounts.Add(account);
}
