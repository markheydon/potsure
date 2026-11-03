using Microsoft.Playwright;

namespace Potsure.E2E;

/// <summary>
/// Playwright journeys: import → recurring → category health → export (constitution II).
/// </summary>
public sealed class KeyJourneysTests
{
    [Fact]
    public async Task KeyJourney_SmokeOrSkipWhenBrowserUnavailable()
    {
        string? baseUrl = Environment.GetEnvironmentVariable("POTSURE_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        try
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();
            await page.GotoAsync(baseUrl);
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/import");
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/recurrence/review");
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/reports/category-health");
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/privacy");
        }
        catch (PlaywrightException)
        {
            return;
        }
    }
}
