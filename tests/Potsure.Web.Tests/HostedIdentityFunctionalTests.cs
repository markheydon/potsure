using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Data;
using Potsure.Data.Identity;
using Potsure.Data.Tenancy;
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
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor("hosted-smoke-owner"));
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase($"potsure-hosted-identity-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddHostedIdentity(configuration);
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
