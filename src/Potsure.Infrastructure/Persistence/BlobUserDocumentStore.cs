using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Potsure.Data.Documents;

namespace Potsure.Infrastructure.Persistence;

public sealed class BlobUserDocumentStore(BlobContainerClient containerClient) : IUserDocumentStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public async Task<DocumentLoadResult> LoadAsync(string userId, CancellationToken cancellationToken = default)
    {
        UserDocumentPathValidator.ValidateUserId(userId);
        string blobName = UserDocumentPathValidator.StateBlobName(userId);
        BlobClient blob = containerClient.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            return new DocumentLoadResult
            {
                Document = CreateEmptyDocument(userId),
                ETag = null,
                IsNew = true,
            };
        }

        Response<BlobDownloadResult> download = await blob.DownloadContentAsync(cancellationToken);
        string json = download.Value.Content.ToString();
        UserStateDocument? document = JsonSerializer.Deserialize<UserStateDocument>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialise user state document.");

        bool migrated = document.SchemaVersion < UserStateDocument.CurrentSchemaVersion;
        document = DocumentSchemaMigrator.MigrateIfNeeded(document, userId);

        string? etag = download.Value.Details.ETag.ToString();
        if (migrated)
        {
            await SaveAsync(userId, document, etag, cancellationToken);
            Response<BlobProperties> props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
            etag = props.Value.ETag.ToString();
        }

        return new DocumentLoadResult
        {
            Document = document,
            ETag = etag,
            IsNew = false,
        };
    }

    public async Task SaveAsync(
        string userId,
        UserStateDocument document,
        string? ifMatchEtag,
        CancellationToken cancellationToken = default)
    {
        UserDocumentPathValidator.ValidateUserId(userId);
        if (!string.Equals(document.UserId, userId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Document user id must match the save target.", nameof(document));
        }

        document.SchemaVersion = UserStateDocument.CurrentSchemaVersion;
        string blobName = UserDocumentPathValidator.StateBlobName(userId);
        BlobClient blob = containerClient.GetBlobClient(blobName);
        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document, SerializerOptions));

        BlobUploadOptions options = new()
        {
            Conditions = ifMatchEtag is null
                ? new BlobRequestConditions { IfNoneMatch = new ETag("*") }
                : new BlobRequestConditions { IfMatch = new ETag(ifMatchEtag) },
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/json; charset=utf-8" },
        };

        try
        {
            await blob.UploadAsync(new BinaryData(payload), options, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status is 409 or 412)
        {
            throw new DocumentStoreConcurrencyException();
        }
    }

    public async Task DeleteStateAsync(string userId, CancellationToken cancellationToken = default)
    {
        UserDocumentPathValidator.ValidateUserId(userId);
        BlobClient blob = containerClient.GetBlobClient(UserDocumentPathValidator.StateBlobName(userId));
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> StateExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        UserDocumentPathValidator.ValidateUserId(userId);
        BlobClient blob = containerClient.GetBlobClient(UserDocumentPathValidator.StateBlobName(userId));
        Response<bool> exists = await blob.ExistsAsync(cancellationToken);
        return exists.Value;
    }

    private static UserStateDocument CreateEmptyDocument(string userId) => new()
    {
        SchemaVersion = UserStateDocument.CurrentSchemaVersion,
        UserId = userId,
    };
}
