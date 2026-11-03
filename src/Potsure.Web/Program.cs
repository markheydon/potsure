using BlazorBlueprint.Components;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Persistence;
using Potsure.Web.Components;
using Potsure.Web.Configuration;
using Potsure.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<DeploymentModeOptions>(
    builder.Configuration.GetSection(DeploymentModeOptions.SectionName));
builder.Services.AddSingleton<ReauthenticationService>();

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
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

var deployment = builder.Configuration.GetSection(DeploymentModeOptions.SectionName).Get<DeploymentModeOptions>()
    ?? new DeploymentModeOptions();
if (deployment.Mode == DeploymentMode.LocalOnly && !deployment.BindPublicEndpoints)
{
    app.Urls.Clear();
    app.Urls.Add("http://127.0.0.1:0");
}

app.Run();
