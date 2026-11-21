using Microsoft.Playwright;

namespace Potsure.E2E;

/// <summary>
/// Playwright journeys: import → recurring → category health → export (constitution II).
/// </summary>
[Collection(AspireE2eAppHostSuite.Name)]
public sealed class KeyJourneysTests(AspireE2eAppHostFixture appHost)
{
    [Fact]
    public async Task KeyJourney_ImportRecurringCategoryHealthExport_WhenBaseUrlConfigured()
    {
        string baseUrl = appHost.WebBaseUrl.ToString().TrimEnd('/');
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

        const string testName = nameof(KeyJourney_ImportRecurringCategoryHealthExport_WhenBaseUrlConfigured);
        bool failed = false;

        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightE2eDiagnostics.StartTracingAsync(context, testName);

        try
        {
            IPage page = await context.NewPageAsync();

            await PlaywrightSession.EnsureCanReachImportAsync(page, baseUrl);
            string origin = PlaywrightSession.GetPageOrigin(page);

            await ImportJourney.UploadMonzoFixtureAndImportAsync(page, fixturePath);

            await page.GotoAsync($"{origin}/recurrence/review");
            await page.Locator("h1").Filter(new() { HasText = "Review recurring patterns" }).WaitForAsync(new() { Timeout = 60_000 });
            ILocator confirmButton = page.GetByRole(AriaRole.Button, new() { Name = "Confirm" }).First;
            if (await confirmButton.CountAsync() > 0)
            {
                await confirmButton.ClickAsync();
            }

            await page.GotoAsync($"{origin}/reports/category-health");
            await page.Locator("h1").Filter(new() { HasText = "Category health" }).WaitForAsync();

            await page.GotoAsync($"{origin}/privacy", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await page.Locator("h1").Filter(new() { HasText = "Export and delete data" }).WaitForAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Export data" }).ClickAsync();

            ILocator exportConfirm = page.Locator("[aria-label='Confirm export']");
            ILocator exportReady = page.GetByText("Export ready", new() { Exact = false });
            ILocator exportFailed = page.GetByText("Export failed", new() { Exact = false });
            await exportConfirm.Or(exportReady).Or(exportFailed).First.WaitForAsync(new() { Timeout = 120_000 });

            if (await exportConfirm.IsVisibleAsync())
            {
                ILocator hostedPassword = exportConfirm.GetByLabel("Current password");
                ILocator exportPhrase = exportConfirm.GetByPlaceholder("EXPORT");
                string password = Environment.GetEnvironmentVariable("POTSURE_E2E_PASSWORD") ?? "Password1";

                if (await hostedPassword.CountAsync() > 0)
                {
                    await hostedPassword.FillAsync(password);
                }
                else
                {
                    await exportPhrase.FillAsync("EXPORT");
                }

                await exportConfirm.GetByRole(AriaRole.Button, new() { Name = "Continue export" }).ClickAsync();
            }

            ILocator exportOutcome = exportReady
                .Or(exportFailed)
                .Or(page.GetByRole(AriaRole.Link, new() { NameRegex = new System.Text.RegularExpressions.Regex("Download potsure-export", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }));
            await exportOutcome.First.WaitForAsync(new() { Timeout = 120_000 });

            if (await exportFailed.IsVisibleAsync())
            {
                failed = true;
                string exportDetails = await PlaywrightE2eDiagnostics.DescribePageStateAsync(page);
                Assert.Fail($"Export failed on the privacy page.{Environment.NewLine}{exportDetails}");
            }
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
            failed = true;
            IPage? page = context.Pages.Count > 0 ? context.Pages[0] : null;
            string details = page is null
                ? string.Empty
                : await PlaywrightE2eDiagnostics.DescribePageStateAsync(page);

            Assert.Fail($"Playwright journey failed: {ex.Message}{Environment.NewLine}{details}");
        }
        finally
        {
            await PlaywrightE2eDiagnostics.StopTracingOnFailureAsync(
                context,
                PlaywrightE2eDiagnostics.SanitizeTraceFileName(testName),
                failed);
        }
    }
}
