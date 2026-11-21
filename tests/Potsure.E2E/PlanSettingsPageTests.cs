using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Potsure.E2E;

public sealed class PlanSettingsPageTests
{
    [Fact]
    public async Task PlanSettings_LoadsAfterSignIn_WhenBaseUrlConfigured()
    {
        string? baseUrl = Environment.GetEnvironmentVariable("POTSURE_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Assert.Skip("Set POTSURE_E2E_BASE_URL to run Playwright journeys (see docs/e2e-testing.md).");
        }

        string trimmed = baseUrl.TrimEnd('/');

        using IPlaywright playwright = await Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });
        IPage page = await context.NewPageAsync();

        await PlaywrightSession.EnsureCanReachImportAsync(page, trimmed);

        await page.GotoAsync($"{trimmed}/", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });

        await page.GetByRole(AriaRole.Link, new() { Name = "Plan settings" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($"{Regex.Escape(trimmed)}/plan"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Save plan" }).WaitForAsync(new() { Timeout = 45_000 });
    }
}