using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Potsure.Infrastructure.Persistence;

/// <summary>
/// SQL persistence for ASP.NET Core Data Protection keys without tenancy-scoped dependencies.
/// </summary>
public sealed class PotsureDataProtectionDbContext(DbContextOptions<PotsureDataProtectionDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataProtectionKey>(entity =>
        {
            entity.ToTable("DataProtectionKeys");
        });
    }
}
