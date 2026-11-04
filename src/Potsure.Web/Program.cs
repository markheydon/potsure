using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Potsure.Data;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Tenancy;
using Potsure.Web.Components;
using Potsure.Web.Components.Account;
using Potsure.Web.Configuration;
using Potsure.Web.Services;
using Potsure.Web.Telemetry;
using Potsure.Web.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.ConfigurePotsureOpenTelemetry();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<DeploymentModeOptions>(
    builder.Configuration.GetSection(DeploymentModeOptions.SectionName));

DeploymentModeOptions deployment = builder.Configuration
    .GetSection(DeploymentModeOptions.SectionName)
    .Get<DeploymentModeOptions>() ?? new DeploymentModeOptions();

DeploymentModeStartupValidation.EnsureValid(deployment);

if (deployment.Mode == DeploymentMode.LocalOnly)
{
    builder.Services.AddSingleton<ICurrentOwnerAccessor, LocalOnlyOwnerAccessor>();
    builder.Services.AddAuthorization();
    builder.Services.AddCascadingAuthenticationState();
}
else if (deployment.Mode == DeploymentMode.Hosted)
{
    builder.Services.AddSingleton<ICurrentOwnerAccessor, HttpContextOwnerAccessor>();
    builder.Services.AddHostedIdentity(builder.Configuration);
}

builder.Services.AddScoped<ReauthenticationService>();
builder.Services.AddScoped<PasswordReauthenticationService>();
builder.Services.AddScoped<Potsure.Infrastructure.Export.IExportReauthenticationGate, ExportReauthenticationGateAdapter>();
builder.Services.AddPotsureInfrastructureServices();
builder.AddSqlServerDbContext<PotsureDbContext>("potsure");
builder.AddSqlServerDbContext<PotsureDataProtectionDbContext>(
    "potsure",
    configureDbContextOptions: options =>
        options.UseSqlServer(
            sql => sql.MigrationsHistoryTable("__EFDataProtectionMigrationsHistory")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PotsureDbContext>("sql", tags: ["ready"]);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorBlueprintComponents(configureTheme: options =>
{
    options.DefaultRadius = 0.65;
    options.PersistToLocalStorage = true;
    options.DetectSystemPreference = true;
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

if (deployment.Mode == DeploymentMode.Hosted)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;

        if (builder.Environment.IsDevelopment())
        {
            // Dev tunnels, GitHub Codespaces, and Aspire proxies are not in default known-proxy lists.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        }
    });
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (deployment.Mode == DeploymentMode.Hosted)
{
    app.UseForwardedHeaders();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (deployment.Mode == DeploymentMode.Hosted)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (deployment.Mode == DeploymentMode.Hosted)
{
    app.MapAdditionalIdentityEndpoints();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
}).AllowAnonymous();
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
}).AllowAnonymous();

if (deployment.Mode == DeploymentMode.LocalOnly && !deployment.BindPublicEndpoints)
{
    app.Urls.Clear();
    app.Urls.Add("http://127.0.0.1:0");
}

app.Run();
