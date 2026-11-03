using OpenTelemetry.Instrumentation.AspNetCore;

namespace Potsure.Web.Telemetry;

/// <summary>
/// Redacts sensitive import, export, and account paths from ASP.NET Core trace instrumentation.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IHostApplicationBuilder ConfigurePotsureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
        {
            Func<HttpContext, bool>? previous = options.Filter;
            options.Filter = context =>
            {
                PathString path = context.Request.Path;
                if (path.StartsWithSegments("/import")
                    || path.StartsWithSegments("/privacy")
                    || path.StartsWithSegments("/account"))
                {
                    return false;
                }

                return previous?.Invoke(context) ?? true;
            };
        });

        return builder;
    }
}
