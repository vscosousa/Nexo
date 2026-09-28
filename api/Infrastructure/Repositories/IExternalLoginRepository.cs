using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IExternalLoginRepository
{
    /// <summary>Finds the link for the provider identity, or null if none exists.</summary>
    Task<ExternalLogin?> FindAsync(string provider, string providerKey);

    /// <summary>Tracks the link for insertion; nothing is written until the context is saved.</summary>
    void Add(ExternalLogin login);
}
