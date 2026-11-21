using Microsoft.Extensions.DependencyInjection;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Features.Export;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Tests;

public static class DocumentStoreTestHarness
{
    public static ServiceProvider BuildProvider(
        string ownerKey,
        string? dataDirectory = null,
        IExportReauthenticationGate? reauthenticationGate = null)
    {
        dataDirectory ??= Path.Combine(Path.GetTempPath(), "potsure-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(dataDirectory);

        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        if (reauthenticationGate is not null)
        {
            services.AddSingleton(reauthenticationGate);
        }

        services.AddPotsureInfrastructureServices();
        services.AddPotsureFileDocumentStores(options => options.DataDirectory = dataDirectory);
        return services.BuildServiceProvider();
    }

    public sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
