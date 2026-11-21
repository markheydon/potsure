# End-to-end (Playwright) testing

Potsure ships smoke and journey tests in `tests/Potsure.E2E/` using [Playwright for .NET](https://playwright.dev/dotnet/).

The E2E project references `Potsure.AppHost` and uses [Aspire.Hosting.Testing](https://aspire.dev/testing/write-your-first-test/) (`DistributedApplicationTestingBuilder`) to start the distributed application, wait until the `web` resource is healthy, and obtain the live URL with `GetEndpoint("web")`. Playwright navigates to that endpoint. No `POTSURE_E2E_BASE_URL` and no hard-coded ports.

To run unit and integration tests without starting the AppHost:

```bash
dotnet test Potsure.slnx --filter "FullyQualifiedName!~Potsure.E2E"
```

## Local full stack (recommended)

One command builds the E2E project, installs Chromium (with Linux system dependencies when needed), and runs journeys:

```bash
./scripts/run-e2e-playwright.sh
```

Requirements: Docker running (Azurite emulator for hosted blob storage), the Aspire CLI (so the AppHost CLI bundle resolves at build time), and PowerShell (`pwsh`) for the Playwright install script.

Optional environment variables:

| Variable | Purpose |
|----------|---------|
| `POTSURE_E2E_EMAIL` / `POTSURE_E2E_PASSWORD` | Fixed hosted Identity credentials for debugging (default: ephemeral register per run, password `Password1`) |
| `PLAYWRIGHT_TRACE_DIR` | Directory for failure trace zip files (default: repository `playwright-traces/`) |

## Manual local workflow

1. Ensure Docker is running.
2. Build and install browsers:

   ```bash
   dotnet build tests/Potsure.E2E/Potsure.E2E.csproj -c Release
   pwsh tests/Potsure.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps
   ```

3. Run the E2E project:

   ```bash
   dotnet test tests/Potsure.E2E/Potsure.E2E.csproj -c Release
   ```

## CI

[`.github/workflows/playwright.yml`](../.github/workflows/playwright.yml) runs on every pull request and push to `main` (alongside [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)). It invokes `scripts/run-e2e-playwright.sh`, which matches the manual flow above.

On failure, CI uploads Playwright traces from `playwright-traces/` (open with `pwsh .../playwright.ps1 show-trace <file>.zip` or [trace.playwright.dev](https://trace.playwright.dev)).

Configure branch protection on `main` to require both **CI** and **Playwright E2E** when you want merges blocked on browser journeys.

## Journey coverage

`KeyJourneysTests` registers a hosted user when needed, uploads `tests/Potsure.Statements.Tests/Fixtures/monzo-golden-valid.csv`, confirms import mapping (or account confirmation when required), confirms a recurring pattern when offered, visits category health, and completes export reauthentication (password in hosted mode, `EXPORT` phrase in local-only mode).