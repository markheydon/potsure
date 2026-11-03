using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Potsure.Data;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Tenancy;
using Potsure.Web;
using Potsure.Web.Components;
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

if (deployment.Mode == DeploymentMode.LocalOnly)
{
    builder.Services.AddSingleton<ICurrentOwnerAccessor, LocalOnlyOwnerAccessor>();
    builder.Services.AddAuthorization();
    builder.Services.AddCascadingAuthenticationState();
}
if (deployment.Mode == DeploymentMode.Hosted)
{
    builder.Services.AddSingleton<ICurrentOwnerAccessor, HttpContextOwnerAccessor>();
    builder.Services.AddHostedPasskeyIdentity(builder.Configuration);
}

builder.Services.AddScoped<PasskeyStepUpJsInterop>();
builder.Services.AddScoped<PasskeySignInJsInterop>();

builder.Services.AddScoped<ReauthenticationService>();
builder.Services.AddScoped<Potsure.Infrastructure.Export.IExportReauthenticationGate, ExportReauthenticationGateAdapter>();
builder.Services.AddPotsureInfrastructureServices();
builder.AddSqlServerDbContext<PotsureDbContext>("potsure");
builder.AddSqlServerDbContext<PotsureDataProtectionDbContext>(
    "potsure",
    configureDbContextOptions: options =>
        options.UseSqlServer(
            sql => sql.MigrationsHistoryTable("__EFDataProtectionMigrationsHistory")));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorBlueprintComponents();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

if (deployment.Mode == DeploymentMode.Hosted)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<HostedFinancialDataAuthorizationMiddleware>();
    app.MapHostedPasskeySignInEndpoints();
    app.MapHostedPasskeyStepUpEndpoints();
}

app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHealthChecks("/health").AllowAnonymous();
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
