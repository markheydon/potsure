namespace Potsure.Web.Telemetry;

/// <summary>
/// Potsure-specific OpenTelemetry hooks (import/export path redaction removed — rely on platform sampling and log redaction).
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IHostApplicationBuilder ConfigurePotsureOpenTelemetry(this IHostApplicationBuilder builder) =>
        builder;
}
