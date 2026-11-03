# Encryption

Potsure v1 expects encryption **in transit** for hosted deployments and **at rest** for the SQL Server database. Local-only loopback binding reduces transit exposure but does not remove the need to protect database files at rest on your machine.

## In transit (hosted)

| Control | Implementation | Verification |
|---------|----------------|--------------|
| HTTPS/TLS ingress | Azure Container Apps external HTTP endpoints with platform-managed TLS | Confirm browser shows valid HTTPS; HSTS enabled outside Development (`UseHsts` in `Program.cs`) |
| HTTP → HTTPS | `UseHttpsRedirection` in the web host | Request to `http://` redirects to `https://` in production configurations |
| Telemetry | OpenTelemetry configured without upload bodies or export payloads | See `OpenTelemetryExtensions` sanitisation |

### Transit checklist

- [ ] Ingress terminates TLS 1.2 or later.
- [ ] Application redirects HTTP to HTTPS in non-development environments.
- [ ] No financial export or statement bodies appear in structured logs.

## At rest (hosted)

| Control | Implementation | Verification |
|---------|----------------|--------------|
| Azure SQL TDE | Azure SQL Server resource in AppHost (`AddAzureSqlServer`) | Confirm TDE enabled on the logical server in Azure Portal or policy audit |
| Data Protection keys | Persisted to SQL via `PersistKeysToDbContext` | Keys table present after first run |
| Secrets | Connection strings via Aspire/configuration, not committed to source | Repository secret scan clean |

### At rest checklist

- [ ] Database encrypted at rest (TDE or equivalent for self-managed SQL).
- [ ] Backups encrypted with the same policy as the primary database.
- [ ] Export files stored offline by the user are handled according to their own device encryption practices.

## Local-only note

Loopback binding limits network transit; protect the SQL instance or files with OS-level disk encryption and restricted file permissions.

## Acceptance tests

Automated acceptance checks in `tests/Potsure.Infrastructure.Tests/HostedEncryptionAcceptanceTests.cs` assert that documentation, AppHost, and web host sources include the expected HTTPS and Azure SQL configuration hooks. Complete the manual checklists above before production hosted cut-over (FR-M027, constitution IV).
