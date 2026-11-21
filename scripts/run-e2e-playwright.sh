#!/usr/bin/env bash
# Builds tests/Potsure.E2E, installs Playwright Chromium, and runs browser journeys.
# The E2E project starts Potsure.AppHost via Aspire.Hosting.Testing and reads the web endpoint from GetEndpoint("web").
# See docs/e2e-testing.md.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
E2E_PROJECT="$REPO_ROOT/tests/Potsure.E2E/Potsure.E2E.csproj"
PLAYWRIGHT_SCRIPT="$REPO_ROOT/tests/Potsure.E2E/bin/Release/net10.0/playwright.ps1"

usage() {
  echo "Usage: $0" >&2
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h | --help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Required command not found: $1" >&2
    exit 1
  fi
}

require_command dotnet
require_command docker
require_command pwsh

if ! docker info >/dev/null 2>&1; then
  echo "Docker must be running for AppHost storage (Azurite emulator)." >&2
  exit 1
fi

if command -v aspire >/dev/null 2>&1; then
  echo "Preparing Aspire CLI bundle (required to build Potsure.AppHost)..."
  aspire setup --non-interactive
fi

echo "Building E2E project (Release)..."
dotnet restore "$E2E_PROJECT"
dotnet build "$E2E_PROJECT" -c Release --no-restore

PLAYWRIGHT_INSTALL_ARGS=(install chromium)
if [[ "$(uname -s)" == "Linux" ]]; then
  PLAYWRIGHT_INSTALL_ARGS+=(--with-deps)
fi

echo "Installing Playwright Chromium..."
pwsh "$PLAYWRIGHT_SCRIPT" "${PLAYWRIGHT_INSTALL_ARGS[@]}"

export PLAYWRIGHT_TRACE_DIR="$REPO_ROOT/playwright-traces"
mkdir -p "$PLAYWRIGHT_TRACE_DIR"

echo "Running Playwright E2E tests..."
dotnet test "$E2E_PROJECT" -c Release --no-build --report-trx --results-directory "$REPO_ROOT/TestResults/E2E"