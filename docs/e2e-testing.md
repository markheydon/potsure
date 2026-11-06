# End-to-end (Playwright) testing

Potsure ships smoke and journey tests in `tests/Potsure.E2E/` using [Playwright for .NET](https://playwright.dev/dotnet/).

## When tests run

By default, `KeyJourneysTests` **skip** when `POTSURE_E2E_BASE_URL` is unset (`Assert.Skip`) so local `dotnet test Potsure.slnx` reports them as skipped rather than silently passing.

To run unit and integration tests without loading the E2E project logic in your filter:

```bash
dotnet test Potsure.slnx --filter "FullyQualifiedName!~Potsure.E2E"
```

## Optional local full stack (Aspire)

One command starts the AppHost (SQL container, EF migrations, hosted `web`), installs Chromium, discovers the `web` URL, and runs the journey tests:

```bash
./scripts/run-e2e-playwright.sh
```

Requirements: Docker running, [Aspire CLI](https://aspire.dev/get-started/install-cli/), `jq`, and PowerShell (`pwsh`) for the Playwright install script.

If the app is already running, skip AppHost lifecycle:

```bash
export POTSURE_E2E_BASE_URL=http://127.0.0.1:5191
./scripts/run-e2e-playwright.sh --use-existing-base-url
```

Optional environment variables:

| Variable | Purpose |
|----------|---------|
| `POTSURE_APPHOST` | Path to AppHost `.csproj` (default: `src/Potsure.AppHost/Potsure.AppHost.csproj`) |
| `POTSURE_E2E_EMAIL` / `POTSURE_E2E_PASSWORD` | Fixed hosted Identity credentials for debugging (default: ephemeral register per run, password `Password1`) |

## Manual local workflow

1. Start the app (`aspire run --apphost src/Potsure.AppHost` or `dotnet run --project src/Potsure.Web` with SQL configured).
2. Ensure migrations are applied (AppHost orchestrates this in development).
3. Export `POTSURE_E2E_BASE_URL` to the web HTTP root (no trailing path).
4. Install Playwright browsers once per machine:

   ```bash
   dotnet build tests/Potsure.E2E/Potsure.E2E.csproj -c Release
   pwsh tests/Potsure.E2E/bin/Release/net10.0/playwright.ps1 install
   ```

5. Run the E2E project:

   ```bash
   dotnet test tests/Potsure.E2E/Potsure.E2E.csproj -c Release
   ```

## CI

[`.github/workflows/playwright.yml`](../.github/workflows/playwright.yml) runs on every pull request and push to `main` (alongside [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)). It invokes `scripts/run-e2e-playwright.sh`, which uses the Aspire CLI to start `Potsure.AppHost`, waits for the `web` resource, sets `POTSURE_E2E_BASE_URL` from `aspire describe`, and runs `tests/Potsure.E2E`.

Configure branch protection on `main` to require both **CI** and **Playwright E2E** when you want merges blocked on browser journeys.

## Journey coverage

`KeyJourneysTests` registers a hosted user when needed, uploads `tests/Potsure.Statements.Tests/Fixtures/monzo-golden-valid.csv`, confirms import mapping (or account confirmation when required), confirms a recurring pattern when offered, visits category health, and completes export reauthentication (password in hosted mode, `EXPORT` phrase in local-only mode).
