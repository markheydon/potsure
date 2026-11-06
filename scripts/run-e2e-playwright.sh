#!/usr/bin/env bash
# Starts Potsure.AppHost (when POTSURE_E2E_BASE_URL is unset), installs Playwright browsers,
# and runs tests/Potsure.E2E. See docs/e2e-testing.md.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APPHOST="${POTSURE_APPHOST:-$REPO_ROOT/src/Potsure.AppHost/Potsure.AppHost.csproj}"
E2E_PROJECT="$REPO_ROOT/tests/Potsure.E2E/Potsure.E2E.csproj"
PLAYWRIGHT_SCRIPT="$REPO_ROOT/tests/Potsure.E2E/bin/Release/net10.0/playwright.ps1"
STARTED_ASPIRE=0
USE_EXISTING_BASE_URL=0

usage() {
  echo "Usage: $0 [--use-existing-base-url]" >&2
  echo "  --use-existing-base-url  Do not start AppHost; require POTSURE_E2E_BASE_URL" >&2
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --use-existing-base-url)
      USE_EXISTING_BASE_URL=1
      shift
      ;;
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

resolve_web_base_url() {
  aspire describe --format Json --non-interactive --apphost "$APPHOST" | jq -r '
    [.resources[]
     | select(.displayName == "web")
     | (.urls[]? | select(.name == "http") | .url),
       (.environment.ASPNETCORE_URLS // "" | split(";")[] | select(startswith("http://")))]
    | map(select(. != null and . != ""))
    | (map(select(test("localhost|127\\.0\\.0\\.1"))) | first) // first // empty
  '
}

cleanup() {
  if [[ "$STARTED_ASPIRE" == "1" ]]; then
    aspire stop --non-interactive --apphost "$APPHOST" || true
  fi
}

trap cleanup EXIT

require_command dotnet
require_command jq
require_command docker
require_command aspire
require_command pwsh

if ! docker info >/dev/null 2>&1; then
  echo "Docker must be running for AppHost SQL (RunAsContainer)." >&2
  exit 1
fi

if [[ "$USE_EXISTING_BASE_URL" == "1" ]]; then
  if [[ -z "${POTSURE_E2E_BASE_URL:-}" ]]; then
    echo "POTSURE_E2E_BASE_URL must be set with --use-existing-base-url." >&2
    exit 1
  fi
  echo "Using existing POTSURE_E2E_BASE_URL=$POTSURE_E2E_BASE_URL"
else
  unset POTSURE_E2E_BASE_URL
  echo "Starting AppHost: $APPHOST"
  aspire start --non-interactive --isolated --apphost "$APPHOST"
  STARTED_ASPIRE=1

  echo "Waiting for web resource..."
  aspire wait web --non-interactive --apphost "$APPHOST"

  POTSURE_E2E_BASE_URL="$(resolve_web_base_url)"
  if [[ -z "$POTSURE_E2E_BASE_URL" || "$POTSURE_E2E_BASE_URL" == "null" ]]; then
    echo "Could not resolve web HTTP URL from aspire describe." >&2
    exit 1
  fi
  export POTSURE_E2E_BASE_URL
  echo "Using POTSURE_E2E_BASE_URL=$POTSURE_E2E_BASE_URL"
fi

echo "Building solution (Release)..."
dotnet restore "$REPO_ROOT/Potsure.slnx"
dotnet build "$REPO_ROOT/Potsure.slnx" -c Release --no-restore

PLAYWRIGHT_INSTALL_ARGS=(install chromium)
if [[ "$(uname -s)" == "Linux" ]]; then
  PLAYWRIGHT_INSTALL_ARGS+=(--with-deps)
fi

echo "Installing Playwright Chromium..."
pwsh "$PLAYWRIGHT_SCRIPT" "${PLAYWRIGHT_INSTALL_ARGS[@]}"

echo "Running Playwright E2E tests..."
dotnet test "$E2E_PROJECT" -c Release --no-build --report-trx --results-directory "$REPO_ROOT/TestResults/E2E"
