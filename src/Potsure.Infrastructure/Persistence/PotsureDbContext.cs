using Microsoft.EntityFrameworkCore;

namespace Potsure.Infrastructure.Persistence;

public sealed class PotsureDbContext(DbContextOptions<PotsureDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PotsureDbContext).Assembly);
    }
}
