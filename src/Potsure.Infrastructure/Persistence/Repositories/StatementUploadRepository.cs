using System.Text.Json;
using Potsure.Data.Entities;
using Potsure.Data.Persistence;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Persistence.Repositories;

public sealed class StatementUploadRepository(
    PotsureUserStateContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    public async Task<StatementUploadEntity> CreatePendingAsync(
        string fileName,
        bool persistChanges = true,
        CancellationToken cancellationToken = default)
    {
        StatementUploadEntity upload = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = ownerAccessor.OwnerKey,
            FileName = fileName,
            Status = StatementUploadStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        dbContext.StatementUploads.Add(upload);
        if (persistChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return upload;
    }

    public async Task CompleteAsync(
        Guid uploadId,
        ImportStatistics statistics,
        IReadOnlyList<string> skippedReasons,
        bool persistChanges = true,
        CancellationToken cancellationToken = default)
    {
        StatementUploadEntity upload = await dbContext.StatementUploads
            .SingleAsync(u => u.Id == uploadId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(upload.OwnerKey, ownerAccessor.OwnerKey);
        upload.Status = StatementUploadStatus.Completed;
        upload.ImportedCount = statistics.ImportedCount;
        upload.SkippedCount = statistics.SkippedCount;
        upload.DedupedCount = statistics.DedupedCount;
        upload.TransferExcludedCount = statistics.TransferExcludedCount;
        upload.SkippedReasonsJson = JsonSerializer.Serialize(skippedReasons);
        if (persistChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task FailAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        StatementUploadEntity upload = await dbContext.StatementUploads
            .SingleAsync(u => u.Id == uploadId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(upload.OwnerKey, ownerAccessor.OwnerKey);
        upload.Status = StatementUploadStatus.Failed;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveRememberedColumnMappingJsonAsync(
        string mappingJson,
        bool persistChanges = true,
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? profile = await dbContext.BudgetProfiles.SingleOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            return;
        }

        profile.RememberedColumnMappingJson = mappingJson;
        if (persistChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string?> GetRememberedColumnMappingJsonAsync(CancellationToken cancellationToken)
    {
        BudgetProfileEntity? profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        return profile?.RememberedColumnMappingJson;
    }
}

public readonly record struct ImportStatistics(
    int ImportedCount,
    int SkippedCount,
    int DedupedCount,
    int TransferExcludedCount);
