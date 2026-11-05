using Microsoft.Playwright;

namespace Potsure.E2E;

internal static class ImportJourney
{
    public static async Task UploadMonzoFixtureAndImportAsync(IPage page, string fixturePath)
    {
        await page.GetByRole(AriaRole.Heading, new() { Name = "Upload CSV" }).WaitForAsync(new() { Timeout = 30_000 });

        ILocator fileInput = page.Locator("input[type='file']");
        await fileInput.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 30_000 });

        bool selected = false;
        for (int attempt = 0; attempt < 3 && !selected; attempt++)
        {
            if (attempt > 0)
            {
                await page.WaitForTimeoutAsync(2_000);
            }

            await fileInput.SetInputFilesAsync(fixturePath);
            try
            {
                await page.GetByText("Selected:", new() { Exact = false }).WaitForAsync(new() { Timeout = 30_000 });
                selected = true;
            }
            catch (TimeoutException)
            {
                if (attempt == 2)
                {
                    throw;
                }
            }
        }

        ILocator mappingButton = page.GetByRole(AriaRole.Button, new() { Name = "Confirm mapping and import" });
        ILocator accountButton = page.GetByRole(AriaRole.Button, new() { Name = "Confirm and import" });
        ILocator readyAction = mappingButton.Or(accountButton);
        await readyAction.First.WaitForAsync(new() { Timeout = 120_000 });

        if (await mappingButton.IsVisibleAsync())
        {
            await mappingButton.ClickAsync();
        }
        else
        {
            await page.Locator("#confirm-account").CheckAsync();
            await accountButton.ClickAsync();
        }

        await page.GetByText("imported", new() { Exact = false }).WaitForAsync(new() { Timeout = 60_000 });
    }
}
