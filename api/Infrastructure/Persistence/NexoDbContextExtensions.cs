using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Exceptions;
using Npgsql;

namespace Nexo.Api.Infrastructure.Persistence;

public static class NexoDbContextExtensions
{
    /// <summary>Saves changes, reporting a unique-index violation (a concurrent duplicate) as a conflict.</summary>
    /// <exception cref="ConflictException">A unique constraint rejected the save.</exception>
    public static async Task SaveChangesOrConflictAsync(this NexoDbContext db, string conflictMessage)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException(conflictMessage);
        }
    }
}
