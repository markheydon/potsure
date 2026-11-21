using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Potsure.Data.Documents;

namespace Potsure.Infrastructure.Persistence;

public sealed class FileUserDocumentStore(IOptions<DocumentStoreOptions> options) : IUserDocumentStore
{
    private readonly DocumentStoreOptions storeOptions = options.Value;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public async Task<DocumentLoadResult> LoadAsync(string userId, CancellationToken cancellationToken = default)
    {
        UserDocumentPathValidator.ValidateUserId(userId);
        string path = GetStatePath(userId);
        if (!File.Exists(path))
        {
            return new DocumentLoadResult
            {
                Document = CreateEmptyDocument(userId),
                ETag = null,
                IsNew = true,
            };
        }

        string json = await File.ReadAllTextAsync(path, cancellationToken);
        string etag = await ComputeFileETagAsync(path, cancellationToken);
        UserStateDocument? document = JsonSerializer.Deserialize<UserStateDocument>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialise user state document.");

        bool migrated = document.SchemaVersion < UserStateDocument.CurrentSchemaVersion;
        document = DocumentSchemaMigrator.MigrateIfNeeded(document, userId);

        if (migrated)
        {
            await SaveAsync(userId, document, etag, cancellationToken);
            etag = await ComputeFileETagAsync(path, cancellationToken);
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
        string path = GetStatePath(userId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path) && ifMatchEtag is not null)
        {
            string currentEtag = await ComputeFileETagAsync(path, cancellationToken);
            if (!string.Equals(currentEtag, ifMatchEtag, StringComparison.Ordinal))
            {
                throw new DocumentStoreConcurrencyException();
            }
        }
        else if (File.Exists(path) && ifMatchEtag is null)
        {
            throw new DocumentStoreConcurrencyException();
        }

        document.SchemaVersion = UserStateDocument.CurrentSchemaVersion;
        string json = JsonSerializer.Serialize(document, SerializerOptions);
        string directory = Path.GetDirectoryName(path)!;
        string tempPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8, cancellationToken);
        File.Move(tempPath, path, overwrite: true);
    }

    public Task DeleteStateAsync(string userId, CancellationToken cancellationToken = default)
    {
        UserDocumentPathValidator.ValidateUserId(userId);
        string path = GetStatePath(userId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<bool> StateExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        UserDocumentPathValidator.ValidateUserId(userId);
        return Task.FromResult(File.Exists(GetStatePath(userId)));
    }

    private string GetStatePath(string userId)
    {
        string relative = UserDocumentPathValidator.StateBlobName(userId).Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(storeOptions.DataDirectory, relative);
    }

    private static UserStateDocument CreateEmptyDocument(string userId) => new()
    {
        SchemaVersion = UserStateDocument.CurrentSchemaVersion,
        UserId = userId,
    };

    private static async Task<string> ComputeFileETagAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        byte[] hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
