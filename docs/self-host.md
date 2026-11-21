# Self-hosting Potsure

Potsure supports **hosted** (multi-user, passkey sign-in) and **local-only** (single profile, no remote account) deployment modes. Configure the mode in `appsettings.json` under the `Deployment` section.

## Deployment modes

| Mode | Typical use | Authentication |
|------|-------------|----------------|
| `Hosted` | Azure Container Apps or your own HTTPS host | ASP.NET Core Identity (email and password) (FR-M025) |
| `LocalOnly` | Personal machine, no network exposure | Fixed local profile key; no sign-in UI |

## Local-only network binding

When `Deployment:Mode` is `LocalOnly` and `Deployment:BindPublicEndpoints` is `false` (default for local-only), Kestrel binds to `http://127.0.0.1` only. The application is not reachable from other machines on your network unless you explicitly change binding.

### Operator checklist (local-only)

1. Set `Deployment:Mode` to `LocalOnly`.
2. Leave `BindPublicEndpoints` as `false` unless you understand the exposure risk.
3. Confirm the process listens on loopback only (for example with `ss -tlnp` or your platform equivalent).
4. Back up the document store directory that holds your plan and imports (see `DocumentStore:DataDirectory` or your blob container).
5. Use **Export and delete** in the app before decommissioning hardware.

## Hosted self-host

For hosted mode on your own infrastructure:

1. Provide HTTPS termination at the ingress (TLS 1.2 or later).
2. Provision a private Azure Blob Storage container for `userdata` (Aspire models this in AppHost; see `docs/deployment-storage.md`).
3. Ensure the web app can reach the container with managed identity (**Storage Blob Data Contributor**).
4. Enable Identity registration and sign-in (email and password for v1 acceptance).
5. Complete the encryption verification checklist in `docs/encryption.md`.

Hosted v1 allows **open registration** (any email can create an account). Internet-facing deployments should add operator controls (invitations, rate limits, or CAPTCHA) before production cut-over.

## Deployment stages (AppHost)

| Stage | Intent | Notes |
|-------|--------|-------|
| Local Aspire | Developer validation | Azurite + `userdata` container; web `WaitFor(userdata)`; see `docs/deployment-storage.md` |
| Dev / test ACA | Hosted smoke | Private storage account, min replicas 0–1, Identity sign-in |
| Production ACA | v1.0.0 gate | TLS ingress, storage service encryption, managed identity to blob container |

Replica policy: financial state is per-user JSON in blob storage; scale web when container access is healthy (quickstart Scenario **8c**).

## Related documentation

- [Local-only mode](./local-only-mode.md)
- [Encryption](./encryption.md)
- [Deployment storage](./deployment-storage.md)
