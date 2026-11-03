using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Persistence;

public sealed class PotsureDbContextFactory : IDesignTimeDbContextFactory<PotsureDbContext>
{
    public PotsureDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Potsure.Web"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string connectionString = configuration.GetConnectionString("potsure")
            ?? "Server=(localdb)\\mssqllocaldb;Database=PotsureDesign;Trusted_Connection=True;TrustServerCertificate=True";

        DbContextOptionsBuilder<PotsureDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);

        return new PotsureDbContext(optionsBuilder.Options, new LocalOnlyOwnerAccessor());
    }
}
