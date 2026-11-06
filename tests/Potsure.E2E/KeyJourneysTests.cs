using Microsoft.Playwright;

namespace Potsure.E2E;

/// <summary>
/// Playwright journeys: import → recurring → category health → export (constitution II).
/// Set <c>POTSURE_E2E_BASE_URL</c>  -  see <c>docs/e2e-testing.md</c>.
/// </summary>
public sealed class KeyJourneysTests
{
    [Fact]
    public async Task KeyJourney_ImportRecurringCategoryHealthExport_WhenBaseUrlConfigured()
    {
        string? baseUrl = Environment.GetEnvironmentVariable("POTSURE_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Assert.Skip("Set POTSURE_E2E_BASE_URL to run Playwright journeys (see docs/e2e-testing.md).");
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

            await PlaywrightSession.EnsureCanReachImportAsync(page, trimmed);

            await ImportJourney.UploadMonzoFixtureAndImportAsync(page, fixturePath);

            await page.GotoAsync($"{trimmed}/recurrence/review");
            await page.Locator("h1").Filter(new() { HasText = "Review recurring patterns" }).WaitForAsync(new() { Timeout = 60_000 });
            ILocator confirmButton = page.GetByRole(AriaRole.Button, new() { Name = "Confirm" }).First;
            if (await confirmButton.CountAsync() > 0)
            {
                await confirmButton.ClickAsync();
            }

            await page.GotoAsync($"{trimmed}/reports/category-health");
            await page.Locator("h1").Filter(new() { HasText = "Category health" }).WaitForAsync();

            await page.GotoAsync($"{trimmed}/privacy");
            await page.Locator("h1").Filter(new() { HasText = "Export and delete data" }).WaitForAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Export data" }).ClickAsync();
            ILocator hostedPassword = page.GetByPlaceholder("Current password");
            ILocator exportPhrase = page.GetByPlaceholder("EXPORT");
            await hostedPassword.Or(exportPhrase).First.WaitForAsync(new() { Timeout = 30_000 });

            if (await hostedPassword.IsVisibleAsync())
            {
                string password = Environment.GetEnvironmentVariable("POTSURE_E2E_PASSWORD") ?? "Password1";
                await hostedPassword.FillAsync(password);
            }
            else
            {
                await exportPhrase.FillAsync("EXPORT");
            }

            await page.GetByRole(AriaRole.Button, new() { Name = "Continue export" }).ClickAsync();
            await page.GetByText("Export ready", new() { Exact = false }).WaitForAsync(new() { Timeout = 30_000 });
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
            Assert.Fail($"Playwright journey failed: {ex.Message}");
        }
    }
}
