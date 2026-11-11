using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Potsure.Data.DesignTime;

/// <summary>
/// Design-time factory for <see cref="PotsureDataProtectionDbContext"/>.
/// </summary>
public sealed class PotsureDataProtectionDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<PotsureDataProtectionDbContext>
{
    public PotsureDataProtectionDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = DesignTimeAspireConnection.LoadConfiguration();
        string connectionString = DesignTimeAspireConnection.Resolve(configuration);

        ServiceCollection services = new();
        services.AddDbContext<PotsureDataProtectionDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__EFDataProtectionMigrationsHistory")));

        ServiceProvider provider = services.BuildServiceProvider(validateScopes: false);
        return provider.GetRequiredService<PotsureDataProtectionDbContext>();
    }
}
