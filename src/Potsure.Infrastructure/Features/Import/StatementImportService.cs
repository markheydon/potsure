using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Features.Recurrence;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Infrastructure.Persistence.Repositories;
using Potsure.Statements;

namespace Potsure.Infrastructure.Features.Import;

#pragma warning disable CA1848, CA1873 // Structured import logging uses standard ILogger extensions

public sealed record StatementImportSummary(
    Guid UploadId,
    int ImportedCount,
    int SkippedCount,
    int DedupedCount,
    int TransferExcludedCount,
    IReadOnlyList<string> SkippedReasons,
    bool RequiresAccountConfirmation,
    string? PostImportWarning);

public sealed record StatementImportPreview(
    MonzoColumnMappingPreview ColumnMapping,
    int ParsedRowCount,
    int ParseErrorCount,
    bool RequiresAccountConfirmation,
    IReadOnlyList<string> PreviewErrors);

public sealed class StatementImportService(
    PotsureDbContext dbContext,
    StatementUploadRepository uploadRepository,
    ICurrentOwnerAccessor ownerAccessor,
    TransactionAttributionService attributionService,
    ExpensePatternService expensePatternService,
    BudgetProfileSeeder profileSeeder,
    ILogger<StatementImportService> logger)
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
        bool userConfirmedAccount,
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

            string? fileDerivedFingerprint = ExtractFileDerivedAccountFingerprint(parseResult);
            MonzoAccountValidationResult accountResult = MonzoAccountValidator.ValidateAssociation(
                fileDerivedFingerprint,
                association?.AccountFingerprint,
                userConfirmedAccount,
                association?.ConfirmedByUser == true);

            bool requiresConfirmation =
                accountResult.Outcome == MonzoAccountValidationOutcome.ConfirmationRequired;

            return new StatementImportPreview(
                mapping,
                parseResult.Rows.Count,
                parseResult.Errors.Count,
                requiresConfirmation,
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
        bool userConfirmedAccount,
        IReadOnlyDictionary<string, string>? columnMap = null,
        CancellationToken cancellationToken = default)
    {
        string? tempPath = null;
        StatementUploadEntity? upload = null;
        bool committed = false;
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

            string? fileDerivedFingerprint = ExtractFileDerivedAccountFingerprint(parseResult);
            MonzoAccountValidationResult accountResult = MonzoAccountValidator.ValidateAssociation(
                fileDerivedFingerprint,
                association?.AccountFingerprint,
                userConfirmedAccount,
                association?.ConfirmedByUser == true);

            if (accountResult.Outcome == MonzoAccountValidationOutcome.ConfirmationRequired)
            {
                logger.LogInformation(
                    "Statement import {UploadId} requires account confirmation for owner {OwnerKey}",
                    upload.Id,
                    ownerAccessor.OwnerKey);
                await uploadRepository.FailAsync(upload.Id, cancellationToken);
                return new StatementImportSummary(
                    upload.Id,
                    0,
                    parseResult.Errors.Count,
                    0,
                    0,
                    skippedReasons,
                    RequiresAccountConfirmation: true,
                    PostImportWarning: null);
            }

            if (accountResult.Outcome == MonzoAccountValidationOutcome.DifferentAccountRejected)
            {
                skippedReasons.Add(accountResult.Detail ?? "Different Monzo account.");
                logger.LogWarning(
                    "Statement import {UploadId} rejected: different Monzo account for owner {OwnerKey}",
                    upload.Id,
                    ownerAccessor.OwnerKey);
                await uploadRepository.FailAsync(upload.Id, cancellationToken);
                return new StatementImportSummary(
                    upload.Id,
                    0,
                    skippedReasons.Count,
                    0,
                    0,
                    skippedReasons,
                    RequiresAccountConfirmation: false,
                    PostImportWarning: null);
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

            ImportStatistics statistics = new(
                entities.Count,
                parseResult.Errors.Count,
                dedupe.DuplicateCount,
                transferExcluded);

            IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
                    await dbContext.Database.BeginTransactionAsync(cancellationToken);

                dbContext.Transactions.AddRange(entities);
                EnsureMonzoAssociation(userConfirmedAccount, fileDerivedFingerprint, ref association);

                await dbContext.SaveChangesAsync(cancellationToken);
                await uploadRepository.CompleteAsync(upload.Id, statistics, skippedReasons, cancellationToken);

                Dictionary<string, string> mapToRemember = columnMap is { Count: > 0 }
                    ? columnMap.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)
                    : BuildDefaultColumnMap(layout);
                RememberedMonzoColumnMapping remembered = new(layout.FileHeaders, mapToRemember);
                await profileSeeder.EnsureProfileAsync(cancellationToken);
                await uploadRepository.SaveRememberedColumnMappingJsonAsync(
                    JsonSerializer.Serialize(remembered),
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                committed = true;
            });

            logger.LogInformation(
                "Statement import {UploadId} completed for owner {OwnerKey}: imported {ImportedCount}, deduped {DedupedCount}, skipped {SkippedCount}",
                upload.Id,
                ownerAccessor.OwnerKey,
                entities.Count,
                dedupe.DuplicateCount,
                statistics.SkippedCount);

            string? postImportWarning = null;
            try
            {
                await attributionService.AttributeAllConfirmedExpensesAsync(cancellationToken);
                await expensePatternService.RefreshNonOverriddenPaymentAnchorsAfterImportAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Statement import {UploadId} post-import attribution failed for owner {OwnerKey}",
                    upload.Id,
                    ownerAccessor.OwnerKey);
                postImportWarning =
                    "Import completed but automatic attribution could not run. You can assign transactions manually.";
            }

            return new StatementImportSummary(
                upload.Id,
                entities.Count,
                statistics.SkippedCount,
                dedupe.DuplicateCount,
                transferExcluded,
                skippedReasons,
                RequiresAccountConfirmation: false,
                postImportWarning);
        }
        catch (Exception ex)
        {
            if (upload is not null)
            {
                logger.LogError(
                    ex,
                    "Statement import {UploadId} failed for owner {OwnerKey}",
                    upload.Id,
                    ownerAccessor.OwnerKey);
            }

            if (upload is not null && !committed)
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

    private void EnsureMonzoAssociation(
        bool userConfirmedAccount,
        string? fileDerivedFingerprint,
        ref MonzoAccountAssociationEntity? association)
    {
        if (!userConfirmedAccount)
        {
            return;
        }

        if (association is null)
        {
            association = new MonzoAccountAssociationEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerAccessor.OwnerKey,
                AccountFingerprint = fileDerivedFingerprint,
                ConfirmedByUser = true,
            };
            dbContext.MonzoAccounts.Add(association);
            return;
        }

        association.ConfirmedByUser = true;
        if (fileDerivedFingerprint is not null && string.IsNullOrWhiteSpace(association.AccountFingerprint))
        {
            association.AccountFingerprint = fileDerivedFingerprint;
        }
    }

    /// <summary>
    /// v1 Monzo CSV exports do not include a dedicated account identifier column; extend when metadata is available.
    /// </summary>
    private static string? ExtractFileDerivedAccountFingerprint(MonzoParseResult parseResult)
    {
        _ = parseResult;
        return null;
    }

    private static Dictionary<string, string> BuildDefaultColumnMap(MonzoColumnMappingPreview layout)
    {
        Dictionary<string, string> mapping = new(StringComparer.Ordinal);
        foreach (string required in layout.RequiredHeaders)
        {
            string match = required;
            foreach (string fileHeader in layout.FileHeaders)
            {
                if (string.Equals(fileHeader, required, StringComparison.OrdinalIgnoreCase))
                {
                    match = fileHeader;
                    break;
                }
            }

            mapping[required] = match;
        }

        return mapping;
    }
}

#pragma warning restore CA1848, CA1873
