using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Data.Identity;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Persistence;
using Potsure.Web.Configuration;

namespace Potsure.Web.Tests;

public sealed class HostedIdentityFunctionalTests
{
    [Fact]
    public async Task AddHostedIdentity_AllowsRegisterAndPasswordSignIn()
    {
        await using ServiceProvider provider = BuildHostedProvider();
        UserManager<ApplicationUser> userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        SignInManager<ApplicationUser> signInManager = provider.GetRequiredService<SignInManager<ApplicationUser>>();

        const string email = "hosted-smoke@example.com";
        const string password = "Password1";

        IdentityResult createResult = await userManager.CreateAsync(new ApplicationUser { UserName = email, Email = email }, password);
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(e => e.Description)));

        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        SignInResult signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        Assert.True(signInResult.Succeeded);
    }

    private static ServiceProvider BuildHostedProvider()
    {
        string dataDirectory = Path.Combine(Path.GetTempPath(), "potsure-hosted-identity", Guid.NewGuid().ToString());
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor("hosted-smoke-owner"));
        services.AddPotsureFileDocumentStores(options => options.DataDirectory = dataDirectory);
        services.AddHostedIdentity(configuration);
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
