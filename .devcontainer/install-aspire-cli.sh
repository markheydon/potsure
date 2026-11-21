#!/usr/bin/env bash
# Installs the Aspire CLI under ~/.aspire/bin and extracts the CLI bundle to a user-writable path.
# Avoids the devcontainer feature that copies aspire to /usr/local (bundle lock permission failures).

set -euo pipefail

curl -fsSL https://aspire.dev/install.sh | bash

export PATH="${HOME}/.aspire/bin:${PATH}"

mkdir -p "${HOME}/.aspire/bundle"
aspire setup --non-interactive --install-path "${HOME}/.aspire/bundle"

aspire --version