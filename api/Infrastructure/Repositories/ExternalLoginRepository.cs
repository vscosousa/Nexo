using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Persistence;

namespace Nexo.Api.Infrastructure.Repositories;

public class ExternalLoginRepository(NexoDbContext db) : IExternalLoginRepository
{
    public Task<ExternalLogin?> FindAsync(string provider, string providerKey) =>
        db.ExternalLogins.SingleOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey);

    public void Add(ExternalLogin login) => db.ExternalLogins.Add(login);
}
