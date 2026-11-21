using System.Net.Http;
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Potsure.E2E;

/// <summary>
/// Starts <see cref="Projects.Potsure_AppHost"/> once per test collection and exposes the live <c>web</c> endpoint.
/// </summary>
public sealed class AspireE2eAppHostFixture : IAsyncLifetime, IAsyncDisposable
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    private IDistributedApplicationTestingBuilder? appHostBuilder;
    private DistributedApplication? application;

    public Uri WebBaseUrl { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        using CancellationTokenSource timeout = new(StartupTimeout);
        CancellationToken cancellationToken = timeout.Token;

        IDistributedApplicationTestingBuilder appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.Potsure_AppHost>(cancellationToken);
        appHostBuilder = appHost;

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Warning);
            logging.AddFilter("Aspire.", LogLevel.Warning);
        });

        appHost.Services.ConfigureHttpClientDefaults(http =>
        {
            http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });
        });

        application = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);

        await application.StartAsync(cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);

        await application.ResourceNotifications
            .WaitForResourceHealthyAsync("web", cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);

        WebBaseUrl = application.GetEndpoint("web", "http");
    }

    public async ValueTask DisposeAsync()
    {
        if (application is not null)
        {
            await application.DisposeAsync();
        }

        if (appHostBuilder is IAsyncDisposable disposableBuilder)
        {
            await disposableBuilder.DisposeAsync();
        }
    }
}
