# Self-hosting Potsure

Potsure supports **hosted** (multi-user, passkey sign-in) and **local-only** (single profile, no remote account) deployment modes. Configure the mode in `appsettings.json` under the `Deployment` section.

## Deployment modes

| Mode | Typical use | Authentication |
|------|-------------|----------------|
| `Hosted` | Azure Container Apps or your own HTTPS host | ASP.NET Core Identity with passkeys (FR-M025) |
| `LocalOnly` | Personal machine, no network exposure | Fixed local profile key; no sign-in UI |

## Local-only network binding

When `Deployment:Mode` is `LocalOnly` and `Deployment:BindPublicEndpoints` is `false` (default for local-only), Kestrel binds to `http://127.0.0.1` only. The application is not reachable from other machines on your network unless you explicitly change binding.

### Operator checklist (local-only)

1. Set `Deployment:Mode` to `LocalOnly`.
2. Leave `BindPublicEndpoints` as `false` unless you understand the exposure risk.
3. Confirm the process listens on loopback only (for example with `ss -tlnp` or your platform equivalent).
4. Back up the SQL database file or instance that stores your plan and imports.
5. Use **Export and delete** in the app before decommissioning hardware.

## Hosted self-host

For hosted mode on your own infrastructure:

1. Provide HTTPS termination at the ingress (TLS 1.2 or later).
2. Configure a SQL Server connection string for `ConnectionStrings:potsure`.
3. Run database migrations idempotently (see `docs/deployment-database.md`).
4. Enable passkey registration and sign-in; do not enable password fallback in v1.
5. Complete the encryption verification checklist in `docs/encryption.md`.

## Deployment stages (AppHost)

| Stage | Intent | Notes |
|-------|--------|-------|
| Local Aspire | Developer validation | SQL container, single web replica, loopback or hosted mode in `appsettings.Development.json` |
| Dev / test ACA | Hosted smoke | Azure SQL free/low tier, min replicas 0–1, passkey sign-in required |
| Production ACA | v1.0.0 gate | TLS ingress, TDE-enabled SQL, migration job before traffic, no password fallback |

Replica policy: avoid running concurrent migration runners; scale web only after schema is current.

## Related documentation

- [Local-only mode](./local-only-mode.md)
- [Encryption](./encryption.md)
- [Deployment database](./deployment-database.md)
