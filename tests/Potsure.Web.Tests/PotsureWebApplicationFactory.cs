using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Identity;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Web.Tests;

public sealed class PotsureWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string dataDirectory = Path.Combine(Path.GetTempPath(), "potsure-waf", Guid.NewGuid().ToString());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Deployment:Mode", "Hosted");
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            RemoveService<IUserDocumentStore>(services);
            RemoveService<IIdentityDocumentStore>(services);
            services.AddPotsureFileDocumentStores(options => options.DataDirectory = dataDirectory);
        });
    }

    private static void RemoveService<TService>(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services.Where(d => d.ServiceType == typeof(TService)).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
