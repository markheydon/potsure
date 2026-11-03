using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Potsure.Web.Configuration;

namespace Potsure.Web.Tests;

public sealed class HostedFinancialDataAuthorizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RedirectsUnauthenticatedHostedImportToSignIn()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DeploymentModeOptions.SectionName}:Mode"] = nameof(DeploymentMode.Hosted),
            })
            .Build();

        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new HostedFinancialDataAuthorizationMiddleware(next, configuration);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/import",
                Scheme = "https",
            },
        };

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/account/sign-in?returnUrl=%2Fimport", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_AllowsLocalOnlyWithoutAuthentication()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DeploymentModeOptions.SectionName}:Mode"] = nameof(DeploymentMode.LocalOnly),
            })
            .Build();

        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new HostedFinancialDataAuthorizationMiddleware(next, configuration);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/import",
            },
        };

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
