# End-to-end (Playwright) testing

Potsure ships smoke and journey tests in `tests/Potsure.E2E/` using [Playwright for .NET](https://playwright.dev/dotnet/).

## When tests run

By default, `KeyJourneysTests` **skip** when `POTSURE_E2E_BASE_URL` is unset (`Assert.Skip`) so local `dotnet test` reports them as skipped rather than silently passing.

Set the environment variable to the base URL of a running instance (no trailing path required), for example:

```bash
export POTSURE_E2E_BASE_URL=http://127.0.0.1:5213
dotnet test tests/Potsure.E2E/Potsure.E2E.csproj -c Release
```

## Local workflow

1. Start the app (Aspire AppHost or `dotnet run --project src/Potsure.Web`).
2. Ensure SQL and migrations are applied (AppHost orchestrates this in development).
3. Export `POTSURE_E2E_BASE_URL` to the web root URL shown in the console.
4. Install Playwright browsers once per machine:

   ```bash
   pwsh tests/Potsure.E2E/bin/Release/net10.0/playwright.ps1 install
   ```

5. Run the E2E project as above.

## CI wiring

Hosted CI is not required to run E2E on every push. When you add a workflow job:

- Provision SQL and start `Potsure.Web` (or AppHost) as a service.
- Set `POTSURE_E2E_BASE_URL` to that service URL.
- Run `dotnet test tests/Potsure.E2E/Potsure.E2E.csproj -c Release` after the app health check passes.

Journey tests upload `tests/Potsure.Statements.Tests/Fixtures/monzo-golden-valid.csv`, confirm import mapping, visit recurring review, category health, and exercise export reauthentication when the UI is reachable.
