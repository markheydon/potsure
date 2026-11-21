# Deployment storage

Potsure v1 persists financial and hosted Identity data in a **private Azure Blob Storage container** (Azurite locally). There is no Azure SQL or Entity Framework migration path on the running application.

## Container

| Setting | Value |
|---------|--------|
| AppHost resource | `AddAzureStorage("storage")` then `AddBlobContainer("userdata", blobContainerName: "potsure-userdata")` |
| Web client | `AddAzureBlobContainerClient("userdata")` (reference name must match AppHost) |
| Local emulator | Azurite with persistent volume `potsure-userdata` |

Blob layout is defined in `specs/001-potsure-v1/contracts/user-state-document.md`.

## Hosted

- Provision a private storage account in the same region as the Container App (UK South).
- Aspire assigns **Storage Blob Data Contributor** to the web app managed identity.
- Use HTTPS only. Encryption at rest is Azure Storage service encryption (Microsoft-managed keys), **FR-M027**.

## Local-only (no AppHost)

When `Deployment:Mode` is `LocalOnly` and the web app is not started under Aspire, files use the same relative paths under `DocumentStore:DataDirectory` (default: `%LocalApplicationData%/Potsure/data`).

## Document schema

`tenants/{userId}/state.json` carries a `schemaVersion` integer. Older documents are upgraded in-process on load. There is no migration job or EF orchestration resource.

## Operator commands

There is no `dotnet ef` step for application data. Deploy the web app after the storage container exists and the AppHost `userdata` reference is healthy (quickstart Scenario **8c**).