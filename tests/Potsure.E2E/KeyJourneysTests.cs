using Microsoft.Playwright;

namespace Potsure.E2E;

/// <summary>
/// Playwright journeys: import → recurring → category health → export (constitution II).
/// Set <c>POTSURE_E2E_BASE_URL</c> in CI or locally to run against a started app instance.
/// </summary>
public sealed class KeyJourneysTests
{
    [Fact]
    public async Task KeyJourney_ImportRecurringCategoryHealthExport_WhenBaseUrlConfigured()
    {
        string? baseUrl = Environment.GetEnvironmentVariable("POTSURE_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        string trimmed = baseUrl.TrimEnd('/');

        try
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();

            await page.GotoAsync(trimmed);
            await page.GotoAsync($"{trimmed}/import");
            ILocator importHeading = page.Locator("#import-heading");
            await importHeading.WaitForAsync();
            Assert.Contains("Import Monzo", await importHeading.InnerTextAsync());

            await page.GotoAsync($"{trimmed}/recurrence/review");
            ILocator reviewHeading = page.Locator("#recurrence-review-heading");
            await reviewHeading.WaitForAsync();
            Assert.Contains("Review recurring", await reviewHeading.InnerTextAsync());

            await page.GotoAsync($"{trimmed}/reports/category-health");
            await page.Locator("h1").Filter(new() { HasText = "Category health" }).WaitForAsync();

            await page.GotoAsync($"{trimmed}/privacy");
            ILocator exportHeading = page.Locator("#export-heading");
            await exportHeading.WaitForAsync();
            Assert.Contains("Export", await exportHeading.InnerTextAsync());
        }
        catch (PlaywrightException ex)
        {
            Assert.Fail($"Playwright journey failed: {ex.Message}");
        }
    }
}
