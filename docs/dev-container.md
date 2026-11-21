# Develop in a Dev Container

Potsure ships a [Dev Container](https://containers.dev/) under `.devcontainer/` for a repeatable .NET 10, Aspire, Docker, Spec Kit, and Playwright environment aligned with how maintainers run the app locally.

## When to use it

Use the Dev Container when you want Docker-in-Docker for Aspire AppHost resources, the Aspire CLI, GitHub and Azure CLIs, Spec Kit, and browser installs without hand-configuring the host. Cursor and VS Code can reopen the folder **inside** the container using `.devcontainer/devcontainer.json`.

**Host requirements** (declared in `devcontainer.json`): 4 CPUs, 8 GB RAM, 32 GB disk.

## Open the workspace

1. Install the Dev Containers extension (VS Code / Cursor) or use the `devcontainer` CLI.
2. Open the repository root and run **Reopen in Container** (or equivalent).
3. Wait for `onCreateCommand`, `postCreateCommand`, and `postStartCommand` to finish on first create.

Included **features** (defaults from each feature image): Aspire 3, Docker-in-Docker, GitHub CLI, Azure CLI, Spec Kit 1, dev-agent-tools, Cursor CLI. Locked image digests live in `.devcontainer/devcontainer-lock.json`.

## HTTPS development certificates

ASP.NET Core and Aspire use HTTPS in development. The container wires trust automatically instead of copying a public cert into the repo for a Windows host import.

### On first create (`onCreateCommand`)

1. `.devcontainer/setup-dotnet-dev-cert.sh`  
   - Ensures `/home/vscode/.dotnet` is owned by `vscode`.  
   - Creates the ASP.NET HTTPS dev certificate if missing (`dotnet dev-certs https`).  
   - Exports it to `/usr/local/share/ca-certificates/dotnet-dev-cert.crt` and runs `update-ca-certificates` so Linux tooling trusts it.
2. `apt-get install libnss3-tools` for NSS/certificate utilities used by Chromium (Playwright).

### Persisted certificate store

A Docker volume named `x509stores` is mounted at `/home/vscode/.dotnet/corefx/cryptography/x509stores` so the .NET X.509 store (including dev cert keys) survives container rebuilds when the volume is kept.

### On every start

- `postStartCommand`: `aspire certs trust --non-interactive` trusts Aspire-managed development certificates inside the container.
- `remoteEnv.SSL_CERT_DIR` includes `/home/vscode/.aspnet/dev-certs/trust` in addition to the system cert directory for clients that honour `SSL_CERT_DIR`.

### Browsers on the Windows or macOS host

The removed `scripts/export-dev-cert-for-windows-host.sh` workflow (public cert under `.local/aspnetcore-localhost.crt` and `Import-Certificate` on Windows) is **not** part of the current setup. For manual validation from a **host** browser, prefer the **HTTP** web URL shown on the Aspire dashboard (see [specs/001-potsure-v1/quickstart.md](../specs/001-potsure-v1/quickstart.md)) or run the browser inside the Dev Container. HTTPS to forwarded `localhost` ports from the host may still show certificate warnings unless you trust the dev cert on the host yourself.

## Playwright and E2E

`postCreateCommand` runs `.devcontainer/install-playwright-browsers.sh`. For running journeys locally, see [e2e-testing.md](e2e-testing.md).

## Related files

| Path | Role |
|------|------|
| `.devcontainer/devcontainer.json` | Image, features, mounts, lifecycle commands |
| `.devcontainer/setup-dotnet-dev-cert.sh` | HTTPS dev cert generation and system trust |
| `.devcontainer/devcontainer-lock.json` | Pinned feature image versions |