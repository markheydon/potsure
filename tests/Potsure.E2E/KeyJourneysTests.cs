using Microsoft.Playwright;

namespace Potsure.E2E;

/// <summary>
/// Playwright journeys: import → recurring → category health → export (constitution II).
/// Set <c>POTSURE_E2E_BASE_URL</c> — see <c>docs/e2e-testing.md</c>.
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
        string fixturePath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Potsure.Statements.Tests",
                "Fixtures",
                "monzo-golden-valid.csv"));

        if (!File.Exists(fixturePath))
        {
            Assert.Fail($"Monzo fixture not found at {fixturePath}");
        }

        try
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();

            await page.GotoAsync($"{trimmed}/import");
            ILocator importHeading = page.Locator("#import-heading");
            await importHeading.WaitForAsync();
            Assert.Contains("Import Monzo", await importHeading.InnerTextAsync());

            await page.Locator("input[type='file']").SetInputFilesAsync(fixturePath);
            await page.GetByRole(AriaRole.Button, new() { Name = "Confirm mapping and import" }).ClickAsync();
            await page.GetByText("imported", new() { Exact = false }).WaitForAsync(new() { Timeout = 60_000 });

            await page.GotoAsync($"{trimmed}/recurrence/review");
            ILocator reviewHeading = page.Locator("#recurrence-review-heading");
            await reviewHeading.WaitForAsync();
            Assert.Contains("Review recurring", await reviewHeading.InnerTextAsync());

            await page.GotoAsync($"{trimmed}/reports/category-health");
            await page.Locator("h1").Filter(new() { HasText = "Category health" }).WaitForAsync();

            await page.GotoAsync($"{trimmed}/privacy");
            ILocator exportHeading = page.Locator("#export-heading");
            await exportHeading.WaitForAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Export data" }).ClickAsync();
            await page.GetByPlaceholder("EXPORT").FillAsync("EXPORT");
            await page.GetByRole(AriaRole.Button, new() { Name = "Continue export" }).ClickAsync();
            await page.GetByText("Export ready", new() { Exact = false }).WaitForAsync(new() { Timeout = 30_000 });
        }
        catch (PlaywrightException ex)
        {
            Assert.Fail($"Playwright journey failed: {ex.Message}");
        }
    }
}
