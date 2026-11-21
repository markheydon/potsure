# Contract: Per-user JSON documents in Azure Blob Storage

**Feature**: `001-potsure-v1`  
**Date**: 2026-11-20  
**Status**: Normative for the commercial persistence pivot (replaces Azure SQL / EF as the running store)

Conceptual aggregates stay in [data-model.md](../data-model.md). This contract is the physical store: where bytes live, who may read them, and how a later database can replace the store without rewriting budgeting rules.

## Resource

| Concern | Choice |
|---------|--------|
| AppHost | `AddAzureStorage("storage")` then `AddBlobContainer("userdata", blobContainerName: "potsure-userdata")` |
| Local | `RunAsEmulator` (Azurite) with a persistent data volume |
| Hosted | Azure Storage account in the same region as the Container App (UK South). Standard hot blob. No Azure SQL |
| Web client | `AddAzureBlobContainerClient("userdata")` matching the AppHost reference name |
| Client type | Injected `BlobContainerClient` only. Feature code must not build connection strings |

Aspire assigns **Storage Blob Data Contributor** to the web app identity when it provisions the account. Hosted access is the blob service URI plus managed identity. The Azurite connection string is injected by AppHost for the emulator only.

The container is private. The browser never receives a blob URL, SAS, or account key. Anonymous and public-container access stay off. Hosted transfer is HTTPS only. Encryption at rest is Azure Storage service encryption with Microsoft-managed keys (the hosted stand-in for the previous SQL TDE check, **FR-M027**).

## Blob layout

One private container. Blob names are chosen by the server from the authenticated owner id, never from a client path.

| Blob | Body | Purpose |
|------|------|---------|
| `tenants/{userId}/state.json` | `UserStateDocument` | All financial aggregates for that user |
| `identity/users/{userId}.json` | Identity account record | Hashed password, security stamp, email, lockout counters. No plaintext password |
| `identity/email-index/{sha256}.json` | `{ "userId": "<id>" }` | Login lookup. `{sha256}` is lowercase hex SHA-256 of the trimmed, upper-invariant email. The email is not the blob name |
| `system/data-protection-keys.xml` | ASP.NET Core Data Protection key ring | Shared app keys, not per user. `PersistKeysToAzureBlobStorage` on this blob |

`userId` is the ASP.NET Core Identity user id. Reject any id that is empty or contains `/`, `\`, or `..` before it is used in a name.

**Local-only mode** (account-free, not network-exposed, web started without AppHost) uses the same relative paths as files under the configured data directory. It does not invent an Azure connection string. AppHost-modelled runs, including local Aspire with `Deployment__Mode=Hosted`, always use the blob client.

## User state document

`state.json` is one JSON object, UTF-8, no raw statement CSV (upload policy is unchanged: stream, parse, discard).

| Field | Rules |
|-------|--------|
| `schemaVersion` | Integer. Current version is **1**. Missing version is treated as 0 and must be migrated before use |
| `userId` | Must equal the blob path owner. Mismatch is a failed read, not a silent repair from caller input |
| Aggregates | Standing plan, categories, pots, Monzo association, transactions, income sources, recurring expenses, assignments, statement-upload metadata. Shapes follow [data-model.md](../data-model.md) |
| Report date range | Not stored (**FR-M031a**, **FR-X006**) |

On load, if `schemaVersion` is older than the code's current version, apply ordered in-process transforms for that document only, then write the new version back with the blob's concurrency condition. There is no database migration resource and no startup job that rewrites every user.

## Concurrency and atomicity

- Each financial save is one conditional upload of the whole `state.json` using the ETag from the read (`If-Match`). A mismatch fails the operation. Do not last-write-wins financial data.
- Import and other multi-step edits change the in-memory document, then replace the blob once. That single replace is the commit. There is no cross-blob transaction.
- Identity blobs and `state.json` are separate. Registration writes identity blobs only. The first financial use creates an empty state document (default categories and preset).
- Permanent delete order: `state.json`, then the email-index blob, then `identity/users/{userId}.json`. Repeat is safe if a blob is already gone. Data-protection keys stay.

## Identity

Hosted sign-in stays ASP.NET Core Identity with cookie authentication and the built-in password hasher. The store is a custom user store over the identity blobs (`IUserStore`, `IUserPasswordStore`, `IUserEmailStore`, `IUserSecurityStampStore`, and lockout if the host enables it). It is not `AddEntityFrameworkStores`.

Email lookup reads only `identity/email-index/{sha256}.json`, then the user blob. Do not list the container to find a user.

## Boundaries

- Budgeting, statement parsing, and recurrence logic stay free of blob types.
- Repositories in `Potsure.Infrastructure` are the feature API. They load and save through a document store. `BlobContainerClient` stays inside that store.
- The store interface is the seam for a future database. Do not keep EF Core, SQL client packages, or `Potsure.MigrationsHost` on the running path "just in case".

## Limits (accepted for this pivot)

The whole tenant document is read and written for each change. That is acceptable for one person and one Monzo account. It is the reason a database returns if the product grows. Splitting one JSON file per transaction is out of scope: it would reintroduce query and atomicity problems without giving a real database.
