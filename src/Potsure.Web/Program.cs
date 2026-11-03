using BlazorBlueprint.Components;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Tenancy;
using Potsure.Web;
using Potsure.Web.Components;
using Potsure.Web.Configuration;
using Potsure.Web.Services;
using Potsure.Web.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.ConfigurePotsureOpenTelemetry();

builder.Services.Configure<DeploymentModeOptions>(
    builder.Configuration.GetSection(DeploymentModeOptions.SectionName));

DeploymentModeOptions deployment = builder.Configuration
    .GetSection(DeploymentModeOptions.SectionName)
    .Get<DeploymentModeOptions>() ?? new DeploymentModeOptions();

if (deployment.Mode == DeploymentMode.LocalOnly)
{
    builder.Services.AddSingleton<ICurrentOwnerAccessor, LocalOnlyOwnerAccessor>();
}
if (deployment.Mode == DeploymentMode.Hosted)
{
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentOwnerAccessor, HttpContextOwnerAccessor>();
    builder.Services.AddHostedPasskeyIdentity(builder.Configuration);
}

builder.Services.AddScoped<PasskeyStepUpJsInterop>();
builder.Services.AddScoped<PasskeySignInJsInterop>();

builder.Services.AddScoped<ReauthenticationService>();
builder.Services.AddScoped<Potsure.Infrastructure.Export.IExportReauthenticationGate, ExportReauthenticationGateAdapter>();
builder.Services.AddPotsureInfrastructureServices();
builder.AddSqlServerDbContext<PotsureDbContext>("potsure");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorBlueprintComponents();

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
    app.MapHostedPasskeyEndpoints();
    app.MapHostedPasskeySignInEndpoints();
    app.MapHostedPasskeyStepUpEndpoints();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

if (deployment.Mode == DeploymentMode.LocalOnly && !deployment.BindPublicEndpoints)
{
    app.Urls.Clear();
    app.Urls.Add("http://127.0.0.1:0");
}

app.Run();
