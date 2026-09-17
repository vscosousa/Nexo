using Microsoft.EntityFrameworkCore;

namespace Nexo.Api.Infrastructure.Persistence;

public class NexoDbContext(DbContextOptions<NexoDbContext> options) : DbContext(options)
{
}
