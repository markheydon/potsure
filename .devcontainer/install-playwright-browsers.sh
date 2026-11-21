#!/usr/bin/env bash
# Installs Chromium for C# Playwright E2E (Microsoft.Playwright via NuGet playwright.ps1).
# No Node.js or npm — see docs/e2e-testing.md.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
E2E_PROJECT="$REPO_ROOT/tests/Potsure.E2E/Potsure.E2E.csproj"
CONFIG="Release"
TFM="net10.0"
PLAYWRIGHT_SCRIPT="$REPO_ROOT/tests/Potsure.E2E/bin/${CONFIG}/${TFM}/playwright.ps1"

if ! command -v pwsh >/dev/null 2>&1; then
  echo "install-playwright-browsers.sh: pwsh is required to run Microsoft.Playwright install." >&2
  exit 1
fi

echo "Building E2E project (Release) for Playwright driver..."
dotnet restore "$E2E_PROJECT"
dotnet build "$E2E_PROJECT" -c "$CONFIG" --no-restore -p:SkipPlaywrightInstall=true

PLAYWRIGHT_INSTALL_ARGS=(install chromium)
if [[ "$(uname -s)" == "Linux" ]]; then
  PLAYWRIGHT_INSTALL_ARGS+=(--with-deps)
fi

echo "Installing Playwright Chromium..."
pwsh "$PLAYWRIGHT_SCRIPT" "${PLAYWRIGHT_INSTALL_ARGS[@]}"

echo "Playwright Chromium install complete."