using Microsoft.Playwright;

namespace Potsure.E2E;

/// <summary>
/// Hosted-mode sign-in for E2E when AppHost sets <c>Deployment__Mode=Hosted</c>.
/// Local-only deployments reach import without authentication.
/// </summary>
internal static class PlaywrightSession
{
    private const string DefaultPassword = "Password1";

    public static async Task EnsureCanReachImportAsync(IPage page, string baseUrl)
    {
        string trimmed = baseUrl.TrimEnd('/');
        await page.GotoAsync($"{trimmed}/import");

        if (await NeedsHostedSignInAsync(page))
        {
            (string email, string password) = ResolveCredentials();
            await RegisterAsync(page, trimmed, email, password);
            await page.GotoAsync($"{trimmed}/import");
        }

        ILocator importHeading = page.Locator("#import-heading");
        await importHeading.WaitForAsync(new() { Timeout = 30_000 });
        Assert.Contains("Import Monzo", await importHeading.InnerTextAsync());
    }

    private static async Task<bool> NeedsHostedSignInAsync(IPage page)
    {
        if (page.Url.Contains("/Account/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        ILocator loginHeading = page.GetByRole(AriaRole.Heading, new() { Name = "Log in to your account" });
        return await loginHeading.CountAsync() > 0;
    }

    private static (string Email, string Password) ResolveCredentials()
    {
        string? email = Environment.GetEnvironmentVariable("POTSURE_E2E_EMAIL");
        string? password = Environment.GetEnvironmentVariable("POTSURE_E2E_PASSWORD");

        if (string.IsNullOrWhiteSpace(email))
        {
            email = $"e2e-{Guid.NewGuid():N}@potsure.test";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            password = DefaultPassword;
        }

        return (email.Trim(), password);
    }

    private static async Task RegisterAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync($"{baseUrl}/Account/Register");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Create an account" }).WaitForAsync();

        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync(password);
        await page.GetByLabel("Confirm password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Create Account" }).ClickAsync();

        await page.WaitForURLAsync(
            url => !url.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 60_000 });
    }
}
