using System.Globalization;
using System.Text;
using Microsoft.Playwright;

namespace Potsure.E2E;

internal static class PlaywrightE2eDiagnostics
{
    private static string TraceDirectory
    {
        get
        {
            string? fromEnv = Environment.GetEnvironmentVariable("PLAYWRIGHT_TRACE_DIR");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv;
            }

            return Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "playwright-traces"));
        }
    }

    public static async Task StartTracingAsync(
        IBrowserContext context,
        string testDisplayName)
    {
        Directory.CreateDirectory(TraceDirectory);
        await context.Tracing.StartAsync(new()
        {
            Title = testDisplayName,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    public static async Task StopTracingOnFailureAsync(IBrowserContext context, string safeFileName, bool failed)
    {
        if (!failed)
        {
            await context.Tracing.StopAsync();
            return;
        }

        string path = Path.Combine(TraceDirectory, $"{safeFileName}.zip");
        await context.Tracing.StopAsync(new() { Path = path });
    }

    public static string SanitizeTraceFileName(string testDisplayName) =>
        string.Concat(testDisplayName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    public static async Task<string> DescribePageStateAsync(IPage page)
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Page URL: {page.Url}");

        try
        {
            string bodyText = await page.Locator("body").InnerTextAsync(new() { Timeout = 5_000 });
            const int maxLength = 2_000;
            if (bodyText.Length > maxLength)
            {
                bodyText = bodyText[..maxLength] + "…";
            }

            builder.AppendLine("Visible text (truncated):");
            builder.AppendLine(bodyText);
        }
        catch (Exception ex)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"Could not read page text: {ex.Message}");
        }

        return builder.ToString();
    }
}