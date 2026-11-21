using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Potsure.E2E;

[Collection(AspireE2eAppHostSuite.Name)]
public sealed class PlanSettingsPageTests(AspireE2eAppHostFixture appHost)
{
    [Fact]
    public async Task PlanSettings_LoadsAfterSignIn_WhenBaseUrlConfigured()
    {
        string baseUrl = appHost.WebBaseUrl.ToString().TrimEnd('/');

        const string testName = nameof(PlanSettings_LoadsAfterSignIn_WhenBaseUrlConfigured);
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

            await page.GotoAsync($"{origin}/", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });

            await page.GetByRole(AriaRole.Link, new() { Name = "Plan settings" }).ClickAsync();

            await Assertions.Expect(page).ToHaveURLAsync(new Regex("/plan/?$"));
            await page.GetByRole(AriaRole.Button, new() { Name = "Save plan" }).WaitForAsync(new() { Timeout = 45_000 });
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