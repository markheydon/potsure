using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Potsure.Web.Configuration;

namespace Potsure.Web.Tests;

public sealed class HostedIdentityConfigurationTests
{
    [Fact]
    public void AddHostedIdentity_RegistersStandardPasswordPolicy()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddHostedIdentity(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        IdentityOptions options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.Equal(8, options.Password.RequiredLength);
        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireLowercase);
        Assert.True(options.Password.RequireUppercase);
        Assert.False(options.Password.RequireNonAlphanumeric);
    }
}
