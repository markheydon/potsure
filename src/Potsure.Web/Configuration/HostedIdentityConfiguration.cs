using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Identity;

namespace Potsure.Web.Configuration;

/// <summary>
/// Registers ASP.NET Core Identity with passkey support and SQL persistence for hosted mode (FR-M025).
/// </summary>
public static class HostedIdentityConfiguration
{
    /// <summary>
    /// Adds Identity, passkey handler, and cookie authentication for hosted deployment.
    /// </summary>
    public static IServiceCollection AddHostedPasskeyIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 64;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<PotsureDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IPasskeyHandler<ApplicationUser>, PasskeyHandler<ApplicationUser>>();

        services.Configure<IdentityPasskeyOptions>(options =>
        {
            string? publicOrigin = configuration["Deployment:PublicOrigin"];
            if (!string.IsNullOrWhiteSpace(publicOrigin) &&
                Uri.TryCreate(publicOrigin, UriKind.Absolute, out Uri? origin))
            {
                options.ServerDomain = origin.Host;
            }
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        services.AddCascadingAuthenticationState();

        return services;
    }
}
