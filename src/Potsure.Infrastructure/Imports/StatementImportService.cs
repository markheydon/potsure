using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Repositories;
using Potsure.Infrastructure.Persistence.Tenancy;
using Potsure.Infrastructure.Services;
using Potsure.Statements;

namespace Potsure.Infrastructure.StatementImport;

public sealed record StatementImportSummary(
    Guid UploadId,
    int ImportedCount,
    int SkippedCount,
    int DedupedCount,
    int TransferExcludedCount,
    IReadOnlyList<string> SkippedReasons,
    bool RequiresAccountConfirmation,
    string? AccountFingerprint);

public sealed record StatementImportPreview(
    MonzoColumnMappingPreview ColumnMapping,
    int ParsedRowCount,
    int ParseErrorCount,
    bool RequiresAccountConfirmation,
    string? AccountFingerprint,
    IReadOnlyList<string> PreviewErrors);

public sealed class StatementImportService(
    PotsureDbContext dbContext,
    StatementUploadRepository uploadRepository,
    ICurrentOwnerAccessor ownerAccessor,
    TransactionAttributionService attributionService)
{
    public async Task<RememberedMonzoColumnMapping?> GetRememberedColumnMappingAsync(
        CancellationToken cancellationToken = default)
    {
        string? json = await uploadRepository.GetRememberedColumnMappingJsonAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RememberedMonzoColumnMapping>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<StatementImportPreview> PreviewAsync(
        Stream csvStream,
        string _,
        bool confirmAccount,
        string? confirmedFingerprint,
        IReadOnlyDictionary<string, string>? columnMap = null,
        CancellationToken cancellationToken = default)
    {
        string? tempPath = null;
        try
        {
            tempPath = Path.GetTempFileName();
            await using (FileStream tempStream = File.Create(tempPath))
            {
                await csvStream.CopyToAsync(tempStream, cancellationToken);
            }

            await using FileStream readStream = File.OpenRead(tempPath);
            MonzoColumnMappingPreview mapping = await MonzoCsvParser.PreviewColumnMappingAsync(
                readStream,
                cancellationToken);

            readStream.Seek(0, SeekOrigin.Begin);
            MonzoParseResult parseResult = await MonzoCsvParser.ParseAsync(readStream, columnMap, cancellationToken);

            List<string> previewErrors = parseResult.Errors
                .Select(e => $"Row {e.RowNumber}: {e.Reason}")
                .ToList();

            MonzoAccountAssociationEntity? association = await dbContext.MonzoAccounts
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            MonzoAccountValidationResult accountResult = MonzoAccountValidator.ValidateAssociation(
                fileAccountFingerprint: confirmedFingerprint,
                associatedAccountFingerprint: association?.AccountFingerprint,
                explicitConfirmationProvided: confirmAccount);

            bool requiresConfirmation =
                accountResult.Outcome == MonzoAccountValidationOutcome.ConfirmationRequired;

            return new StatementImportPreview(
                mapping,
                parseResult.Rows.Count,
                parseResult.Errors.Count,
                requiresConfirmation,
                confirmedFingerprint,
                previewErrors);
        }
        finally
        {
            if (tempPath is not null && File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task<StatementImportSummary> ImportAsync(
        Stream csvStream,
        string fileName,
        bool confirmAccount,
        string? confirmedFingerprint,
        IReadOnlyDictionary<string, string>? columnMap = null,
        CancellationToken cancellationToken = default)
    {
        string? tempPath = null;
        StatementUploadEntity? upload = null;
        try
        {
            tempPath = Path.GetTempFileName();
            await using (FileStream tempStream = File.Create(tempPath))
            {
                await csvStream.CopyToAsync(tempStream, cancellationToken);
            }

            upload = await uploadRepository.CreatePendingAsync(fileName, cancellationToken);

            await using FileStream readStream = File.OpenRead(tempPath);
            readStream.Seek(0, SeekOrigin.Begin);
            MonzoColumnMappingPreview layout = await MonzoCsvParser.PreviewColumnMappingAsync(
                readStream,
                cancellationToken);
            readStream.Seek(0, SeekOrigin.Begin);
            MonzoParseResult parseResult = await MonzoCsvParser.ParseAsync(readStream, columnMap, cancellationToken);

            List<string> skippedReasons = parseResult.Errors
                .Select(e => $"Row {e.RowNumber}: {e.Reason}")
                .ToList();

            MonzoAccountAssociationEntity? association = await dbContext.MonzoAccounts
                .SingleOrDefaultAsync(cancellationToken);

            MonzoAccountValidationResult accountResult = MonzoAccountValidator.ValidateAssociation(
                fileAccountFingerprint: confirmedFingerprint,
                associatedAccountFingerprint: association?.AccountFingerprint,
                explicitConfirmationProvided: confirmAccount);

            if (accountResult.Outcome == MonzoAccountValidationOutcome.ConfirmationRequired)
            {
                await uploadRepository.FailAsync(upload.Id, cancellationToken);
                return new StatementImportSummary(
                    upload.Id,
                    0,
                    parseResult.Errors.Count,
                    0,
                    0,
                    skippedReasons,
                    RequiresAccountConfirmation: true,
                    confirmedFingerprint);
            }

            if (accountResult.Outcome == MonzoAccountValidationOutcome.DifferentAccountRejected)
            {
                skippedReasons.Add(accountResult.Detail ?? "Different Monzo account.");
                await uploadRepository.FailAsync(upload.Id, cancellationToken);
                return new StatementImportSummary(
                    upload.Id,
                    0,
                    skippedReasons.Count,
                    0,
                    0,
                    skippedReasons,
                    RequiresAccountConfirmation: false,
                    confirmedFingerprint);
            }

            List<TransactionDedupeKey> existingKeys = await dbContext.Transactions
                .AsNoTracking()
                .Select(t => new TransactionDedupeKey(t.DedupeKey, t.MonzoTransactionId != null))
                .ToListAsync(cancellationToken);

            TransactionDeduplicationResult dedupe = TransactionDeduplicator.Deduplicate(parseResult.Rows, existingKeys);

            int transferExcluded = 0;
            List<TransactionEntity> entities = new();
            foreach (MonzoCsvRow row in dedupe.UniqueRows)
            {
                TransactionKind kind = TransactionClassifier.Classify(row);
                if (kind is TransactionKind.InternalTransfer or TransactionKind.PotMovement)
                {
                    transferExcluded++;
                }

                TransactionDedupeKey key = TransactionDeduplicator.CreateKey(row);
                entities.Add(new TransactionEntity
                {
                    Id = Guid.NewGuid(),
                    OwnerKey = ownerAccessor.OwnerKey,
                    MonzoTransactionId = row.MonzoTransactionId,
                    TransactionDate = row.TransactionDate,
                    TransactionTime = row.TransactionTime,
                    Amount = row.Amount,
                    Currency = row.Currency,
                    Type = row.Type,
                    Name = row.Name,
                    Description = row.Description,
                    Kind = kind,
                    UploadBatchId = upload.Id,
                    DedupeKey = key.Value,
                });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.Transactions.AddRange(entities);

            if (confirmAccount && association is null && !string.IsNullOrWhiteSpace(confirmedFingerprint))
            {
                association = new MonzoAccountAssociationEntity
                {
                    Id = Guid.NewGuid(),
                    OwnerKey = ownerAccessor.OwnerKey,
                    AccountFingerprint = confirmedFingerprint.Trim(),
                    ConfirmedByUser = true,
                };
                dbContext.MonzoAccounts.Add(association);
            }
            else if (confirmAccount && association is not null && string.IsNullOrWhiteSpace(association.AccountFingerprint)
                     && !string.IsNullOrWhiteSpace(confirmedFingerprint))
            {
                association.AccountFingerprint = confirmedFingerprint.Trim();
                association.ConfirmedByUser = true;
            }

            ImportStatistics statistics = new(
                entities.Count,
                parseResult.Errors.Count,
                dedupe.DuplicateCount,
                transferExcluded);

            await dbContext.SaveChangesAsync(cancellationToken);
            await uploadRepository.CompleteAsync(upload.Id, statistics, skippedReasons, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Dictionary<string, string> mapToRemember = columnMap is { Count: > 0 }
                ? columnMap.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)
                : BuildDefaultColumnMap(layout);
            RememberedMonzoColumnMapping remembered = new(layout.FileHeaders, mapToRemember);
            await uploadRepository.SaveRememberedColumnMappingJsonAsync(
                JsonSerializer.Serialize(remembered),
                cancellationToken);

            await attributionService.AttributeAllConfirmedExpensesAsync(cancellationToken);

            return new StatementImportSummary(
                upload.Id,
                entities.Count,
                statistics.SkippedCount,
                dedupe.DuplicateCount,
                transferExcluded,
                skippedReasons,
                RequiresAccountConfirmation: false,
                confirmedFingerprint);
        }
        catch
        {
            if (upload is not null)
            {
                await uploadRepository.FailAsync(upload.Id, cancellationToken);
            }

            throw;
        }
        finally
        {
            if (tempPath is not null && File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static Dictionary<string, string> BuildDefaultColumnMap(MonzoColumnMappingPreview layout)
    {
        Dictionary<string, string> mapping = new(StringComparer.Ordinal);
        foreach (string required in layout.RequiredHeaders)
        {
            string match = required;
            foreach (string fileHeader in layout.FileHeaders)
            {
                if (fileHeader == required)
                {
                    match = fileHeader;
                    break;
                }
            }

            if (match == required && layout.FileHeaders.Count > 0)
            {
                match = layout.FileHeaders[0];
            }

            mapping[required] = match;
        }

        return mapping;
    }
}
