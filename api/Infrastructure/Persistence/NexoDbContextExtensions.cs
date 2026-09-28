using Microsoft.EntityFrameworkCore;
using Nexo.Api.Domain.Exceptions;
using Npgsql;

namespace Nexo.Api.Infrastructure.Persistence;

public static class NexoDbContextExtensions
{
    /// <summary>Saves changes, reporting a unique-index violation or a concurrent update of the same row as a conflict.</summary>
    /// <exception cref="ConflictException">A unique constraint or the row's concurrency token rejected the save.</exception>
    public static async Task SaveChangesOrConflictAsync(this NexoDbContext db, string conflictMessage)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (
            e is DbUpdateConcurrencyException
            || e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException(conflictMessage);
        }
    }
}
