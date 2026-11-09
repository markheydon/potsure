using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Data;

namespace Potsure.Web.Tests;

public sealed class PotsureWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Deployment:Mode", "Hosted");
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            RemoveEntityFrameworkCoreRegistrations(services, typeof(PotsureDbContext));
            RemoveEntityFrameworkCoreRegistrations(services, typeof(PotsureDataProtectionDbContext));

            string databaseName = $"potsure-waf-{Guid.NewGuid()}";
            services.AddDbContext<PotsureDbContext>(options =>
                options
                    .UseInMemoryDatabase(databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            services.AddDbContext<PotsureDataProtectionDbContext>(options =>
                options
                    .UseInMemoryDatabase($"{databaseName}-dp")
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }

    private static void RemoveEntityFrameworkCoreRegistrations(IServiceCollection services, Type dbContextType)
    {
        Type optionsType = typeof(DbContextOptions<>).MakeGenericType(dbContextType);
        List<ServiceDescriptor> remove = services
            .Where(d =>
            {
                if (d.ServiceType == dbContextType || d.ServiceType == optionsType)
                {
                    return true;
                }

                if (!d.ServiceType.IsGenericType)
                {
                    return false;
                }

                Type[] args = d.ServiceType.GetGenericArguments();
                if (args.Length != 1 || args[0] != dbContextType)
                {
                    return false;
                }

                return d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true;
            })
            .ToList();

        foreach (ServiceDescriptor descriptor in remove)
        {
            services.Remove(descriptor);
        }
    }
}
