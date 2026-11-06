using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Data.Identity;
using Potsure.Data.Tenancy;

namespace Potsure.Data.DesignTime;

/// <summary>
/// Design-time factory for <see cref="PotsureDbContext"/> (Identity passkey schema + application model).
/// </summary>
public sealed class PotsureDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PotsureDbContext>
{
    public PotsureDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = DesignTimeAspireConnection.LoadConfiguration();
        string connectionString = DesignTimeAspireConnection.Resolve(configuration);

        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor, DesignTimeOwnerAccessor>();
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<PotsureDbContext>();

        services.AddDbContext<PotsureDbContext>(options =>
            options.UseSqlServer(connectionString));

        ServiceProvider provider = services.BuildServiceProvider(validateScopes: false);
        return provider.GetRequiredService<PotsureDbContext>();
    }
}
