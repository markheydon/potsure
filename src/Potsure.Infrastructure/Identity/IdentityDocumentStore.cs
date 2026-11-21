using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Potsure.Data.Documents;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Identity;

/// <summary>
/// Identity blobs on the same container or file root as user state.
/// </summary>
public sealed class BlobIdentityDocumentStore(BlobContainerClient containerClient) : IIdentityDocumentStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<IdentityUserDocument?> LoadUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        IdentityUserLoadResult result = await LoadUserWithETagAsync(userId, cancellationToken);
        return result.User;
    }

    public async Task<IdentityUserLoadResult> LoadUserWithETagAsync(string userId, CancellationToken cancellationToken = default)
    {
        BlobClient blob = containerClient.GetBlobClient(UserDocumentPathValidator.IdentityUserBlobName(userId));
        if (!await blob.ExistsAsync(cancellationToken))
        {
            return new IdentityUserLoadResult { User = null, ETag = null };
        }

        Response<BlobDownloadResult> download = await blob.DownloadContentAsync(cancellationToken);
        IdentityUserDocument? user = JsonSerializer.Deserialize<IdentityUserDocument>(
            download.Value.Content.ToString(),
            SerializerOptions);
        return new IdentityUserLoadResult
        {
            User = user,
            ETag = download.Value.Details.ETag.ToString(),
        };
    }

    public async Task SaveUserAsync(IdentityUserDocument user, string? ifMatchEtag, CancellationToken cancellationToken = default)
    {
        BlobClient blob = containerClient.GetBlobClient(UserDocumentPathValidator.IdentityUserBlobName(user.Id));
        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(user, SerializerOptions));
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

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        BlobClient blob = containerClient.GetBlobClient(UserDocumentPathValidator.IdentityUserBlobName(userId));
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<EmailIndexDocument?> LoadEmailIndexAsync(string normalizedEmailSha256, CancellationToken cancellationToken = default)
    {
        BlobClient blob = containerClient.GetBlobClient(UserDocumentPathValidator.EmailIndexBlobName(normalizedEmailSha256));
        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        Response<BlobDownloadResult> download = await blob.DownloadContentAsync(cancellationToken);
        return JsonSerializer.Deserialize<EmailIndexDocument>(download.Value.Content.ToString(), SerializerOptions);
    }

    public async Task SaveEmailIndexAsync(
        string normalizedEmailSha256,
        EmailIndexDocument index,
        string? ifMatchEtag,
        CancellationToken cancellationToken = default)
    {
        BlobClient blob = containerClient.GetBlobClient(UserDocumentPathValidator.EmailIndexBlobName(normalizedEmailSha256));
        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(index, SerializerOptions));
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

    public async Task DeleteEmailIndexAsync(string normalizedEmailSha256, CancellationToken cancellationToken = default)
    {
        BlobClient blob = containerClient.GetBlobClient(UserDocumentPathValidator.EmailIndexBlobName(normalizedEmailSha256));
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}

public sealed class FileIdentityDocumentStore(IOptions<DocumentStoreOptions> options) : IIdentityDocumentStore
{
    private readonly DocumentStoreOptions storeOptions = options.Value;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<IdentityUserDocument?> LoadUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        IdentityUserLoadResult result = await LoadUserWithETagAsync(userId, cancellationToken);
        return result.User;
    }

    public async Task<IdentityUserLoadResult> LoadUserWithETagAsync(string userId, CancellationToken cancellationToken = default)
    {
        string path = GetPath(UserDocumentPathValidator.IdentityUserBlobName(userId));
        if (!File.Exists(path))
        {
            return new IdentityUserLoadResult { User = null, ETag = null };
        }

        string json = await File.ReadAllTextAsync(path, cancellationToken);
        string etag = await ComputeFileETagAsync(path, cancellationToken);
        IdentityUserDocument? user = JsonSerializer.Deserialize<IdentityUserDocument>(json, SerializerOptions);
        return new IdentityUserLoadResult { User = user, ETag = etag };
    }

    public async Task SaveUserAsync(IdentityUserDocument user, string? ifMatchEtag, CancellationToken cancellationToken = default)
    {
        string path = GetPath(UserDocumentPathValidator.IdentityUserBlobName(user.Id));
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

        string json = JsonSerializer.Serialize(user, SerializerOptions);
        string directory = Path.GetDirectoryName(path)!;
        string tempPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, path, overwrite: true);
    }

    public Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        string path = GetPath(UserDocumentPathValidator.IdentityUserBlobName(userId));
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public async Task<EmailIndexDocument?> LoadEmailIndexAsync(string normalizedEmailSha256, CancellationToken cancellationToken = default)
    {
        string path = GetPath(UserDocumentPathValidator.EmailIndexBlobName(normalizedEmailSha256));
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<EmailIndexDocument>(await File.ReadAllTextAsync(path, cancellationToken), SerializerOptions);
    }

    public async Task SaveEmailIndexAsync(
        string normalizedEmailSha256,
        EmailIndexDocument index,
        string? ifMatchEtag,
        CancellationToken cancellationToken = default)
    {
        string path = GetPath(UserDocumentPathValidator.EmailIndexBlobName(normalizedEmailSha256));
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

        string json = JsonSerializer.Serialize(index, SerializerOptions);
        string directory = Path.GetDirectoryName(path)!;
        string tempPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, path, overwrite: true);
    }

    public Task DeleteEmailIndexAsync(string normalizedEmailSha256, CancellationToken cancellationToken = default)
    {
        string path = GetPath(UserDocumentPathValidator.EmailIndexBlobName(normalizedEmailSha256));
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(string relativeBlobName)
    {
        string relative = relativeBlobName.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(storeOptions.DataDirectory, relative);
    }

    private static async Task<string> ComputeFileETagAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}

public static class IdentityEmailHasher
{
    public static string HashEmailForIndex(string email)
    {
        string normalised = email.Trim().ToUpperInvariant();
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
