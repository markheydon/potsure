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
        ILocator importButton = page.GetByRole(AriaRole.Button, new() { Name = "Import", Exact = true, Disabled = false });
        ILocator readyAction = mappingButton.Or(accountButton).Or(importButton);
        try
        {
            await page.GetByText("Processing your CSV", new() { Exact = false })
                .WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 120_000 });
        }
        catch (TimeoutException)
        {
            // Preview may finish before the spinner is visible in fast environments.
        }

        await readyAction.First.WaitForAsync(new() { Timeout = 120_000 });

        if (await mappingButton.IsVisibleAsync())
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Confirm mapping and import", Disabled = false }).ClickAsync();
            await WaitForImportBusyToFinishAsync(page);
            try
            {
                await page.Locator("label[for='confirm-account']").WaitForAsync(new() { Timeout = 60_000 });
                await page.Locator("label[for='confirm-account']").ClickAsync();
                await page.GetByRole(AriaRole.Button, new() { Name = "Confirm and import", Disabled = false }).ClickAsync();
                await WaitForImportBusyToFinishAsync(page);
            }
            catch (TimeoutException)
            {
                // Account already linked for this user.
            }
        }
        else if (await accountButton.IsVisibleAsync())
        {
            await page.Locator("label[for='confirm-account']").ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Confirm and import", Disabled = false }).ClickAsync();
            await WaitForImportBusyToFinishAsync(page);
        }
        else
        {
            await importButton.ClickAsync();
            await WaitForImportBusyToFinishAsync(page);
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Import another file" })
            .Or(page.GetByText("Import failed", new() { Exact = false }))
            .First.WaitForAsync(new() { Timeout = 120_000 });

        if (await page.GetByText("Import failed", new() { Exact = false }).IsVisibleAsync())
        {
            throw new InvalidOperationException("Import failed before the summary appeared.");
        }
    }

    private static async Task WaitForImportBusyToFinishAsync(IPage page)
    {
        try
        {
            await page.GetByText("Importing", new() { Exact = false })
                .WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 120_000 });
        }
        catch (TimeoutException)
        {
            // Spinner may have been too fast to observe.
        }
    }
}
