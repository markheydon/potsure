# Encryption

Potsure v1 expects encryption **in transit** for hosted deployments and **at rest** for persisted user data. Local-only loopback binding reduces transit exposure but does not remove the need to protect files on disk.

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
| Azure Blob Storage encryption | Private `userdata` container (see [deployment-storage.md](./deployment-storage.md)) | Confirm storage account encryption with Microsoft-managed keys in Azure Portal |
| Data Protection keys | `system/data-protection-keys.xml` in the userdata container (hosted) or local file (local-only) | Blob or file present after first run |
| Secrets | Blob access via Aspire/managed identity, not committed to source | Repository secret scan clean |

### At rest checklist

- [ ] Storage account encrypted at rest (service encryption).
- [ ] Container access is private; no anonymous access.
- [ ] Export files stored offline by the user are handled according to their own device encryption practices.

## Local-only note

Loopback binding limits network transit; protect the data directory under `DocumentStore:DataDirectory` with OS-level disk encryption and restricted file permissions.

## Acceptance tests

Automated acceptance checks in `tests/Potsure.Infrastructure.Tests/HostedEncryptionAcceptanceTests.cs` assert that documentation and web host sources include the expected HTTPS and storage configuration hooks. Complete the manual checklists above before production hosted cut-over (FR-M027, constitution IV).