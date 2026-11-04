using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Potsure.Data;
using Potsure.Data.Identity;
using Potsure.Web.Components.Account;

namespace Potsure.Web.Configuration;

/// <summary>
/// Registers ASP.NET Core Identity with optional passkeys and SQL persistence for hosted mode (FR-M025).
/// </summary>
public static class HostedIdentityConfiguration
{
    /// <summary>
    /// Adds Identity, optional passkey support, and cookie authentication for hosted deployment.
    /// </summary>
    public static IServiceCollection AddHostedIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
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
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

        return services;
    }
}
