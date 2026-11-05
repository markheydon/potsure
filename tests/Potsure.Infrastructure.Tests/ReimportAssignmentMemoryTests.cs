using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Features.Import;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Recurrence;

namespace Potsure.Infrastructure.Tests;

/// <summary>
/// SC-003 / FR-M010a: auto-attribution on import and manual override persistence.
/// </summary>
public sealed class ReimportAssignmentMemoryTests
{
    [Fact]
    public async Task AutoAttributionThenOverride_PreventsReAutoAttributionOnSecondPass()
    {
        const string owner = "test-owner";
        ServiceProvider provider = BuildProvider(owner);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        TransactionAttributionService attribution = scope.ServiceProvider.GetRequiredService<TransactionAttributionService>();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);
        BudgetCategoryEntity needs = await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Needs,
            TestContext.Current.CancellationToken);

        RecurringExpenseEntity expense = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            VendorLabel = "Coffee Shop",
            CategoryId = needs.Id,
            Cadence = ExpenseCadence.Monthly,
            PaymentType = "Card",
            AmountKind = "Fixed",
            FixedAmount = 12m,
            State = PatternConfirmationState.Confirmed,
        };
        db.RecurringExpenses.Add(expense);

        Guid uploadId = Guid.NewGuid();
        db.StatementUploads.Add(new StatementUploadEntity
        {
            Id = uploadId,
            OwnerKey = owner,
            FileName = "test.csv",
            Status = StatementUploadStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        TransactionEntity debit = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = -12m,
            Currency = "GBP",
            Type = "Card payment",
            Name = "Coffee Shop",
            Kind = TransactionKind.ExpenseDebit,
            UploadBatchId = uploadId,
            DedupeKey = "coffee-1",
        };
        db.Transactions.Add(debit);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        int firstPass = await attribution.AttributeMatchingDebitsAsync(
            expense.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, firstPass);
        Assert.NotNull(debit.CategoryId);

        BudgetCategoryEntity wants = await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Wants,
            TestContext.Current.CancellationToken);
        await attribution.OverrideAttributionAsync(
            debit.Id,
            wants.Id,
            null,
            null,
            "manual",
            TestContext.Current.CancellationToken);

        int secondPass = await attribution.AttributeMatchingDebitsAsync(
            expense.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, secondPass);

        await db.Entry(debit).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(wants.Id, debit.CategoryId);

        AssignmentEntity assignment = await db.Assignments.SingleAsync(
            a => a.TransactionId == debit.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal("manual", assignment.Tag);
        Assert.Null(assignment.RecurringExpenseId);
    }

    [Fact]
    public async Task SecondAutoAttributionPass_AfterOverride_DoesNotReassignTransaction()
    {
        const string owner = "test-owner-reimport";
        ServiceProvider provider = BuildProvider(owner);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        TransactionAttributionService attribution = scope.ServiceProvider.GetRequiredService<TransactionAttributionService>();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);
        BudgetCategoryEntity needs = await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Needs,
            TestContext.Current.CancellationToken);

        RecurringExpenseEntity expense = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            VendorLabel = "Corner Cafe",
            CategoryId = needs.Id,
            Cadence = ExpenseCadence.Monthly,
            PaymentType = "Card",
            AmountKind = "Fixed",
            FixedAmount = 3.5m,
            State = PatternConfirmationState.Confirmed,
        };
        db.RecurringExpenses.Add(expense);

        Guid uploadId = Guid.NewGuid();
        db.StatementUploads.Add(new StatementUploadEntity
        {
            Id = uploadId,
            OwnerKey = owner,
            FileName = "first.csv",
            Status = StatementUploadStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        TransactionEntity firstDebit = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = -3.5m,
            Currency = "GBP",
            Type = "Card payment",
            Name = "Corner Cafe",
            Kind = TransactionKind.ExpenseDebit,
            UploadBatchId = uploadId,
            DedupeKey = "cafe-1",
        };
        db.Transactions.Add(firstDebit);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        int firstImportPass = await attribution.AttributeAllConfirmedExpensesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, firstImportPass);

        BudgetCategoryEntity wants = await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Wants,
            TestContext.Current.CancellationToken);
        await attribution.OverrideAttributionAsync(
            firstDebit.Id,
            wants.Id,
            null,
            expense.Id,
            null,
            TestContext.Current.CancellationToken);

        Guid secondUploadId = Guid.NewGuid();
        db.StatementUploads.Add(new StatementUploadEntity
        {
            Id = secondUploadId,
            OwnerKey = owner,
            FileName = "second.csv",
            Status = StatementUploadStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        db.Transactions.Add(new TransactionEntity
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            Amount = -3.5m,
            Currency = "GBP",
            Type = "Card payment",
            Name = "Corner Cafe",
            Kind = TransactionKind.ExpenseDebit,
            UploadBatchId = secondUploadId,
            DedupeKey = "cafe-2",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        int secondImportPass = await attribution.AttributeAllConfirmedExpensesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, secondImportPass);

        await db.Entry(firstDebit).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(wants.Id, firstDebit.CategoryId);
    }

    [Fact]
    public async Task OverlappingImportsThroughStatementImportService_DedupeAndPreserveManualOverride()
    {
        const string owner = "test-owner-import-pipeline";
        const string overlappingCsv =
            """
            Transaction ID,Date,Time,Type,Name,Emoji,Category,Amount,Currency,Local amount,Local currency,Notes and #tags,Address,Receipt,Description,Category split,Money Out,Money In
            tx_pipeline_001,02/10/2026,12:00:00,Card payment,Corner Cafe,,Eating out,-3.50,GBP,,,,,,,,3.50,
            """;

        ServiceProvider provider = BuildProvider(owner);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        StatementImportService importService = scope.ServiceProvider.GetRequiredService<StatementImportService>();
        TransactionAttributionService attribution = scope.ServiceProvider.GetRequiredService<TransactionAttributionService>();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);
        BudgetCategoryEntity needs = await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Needs,
            TestContext.Current.CancellationToken);
        BudgetCategoryEntity wants = await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Wants,
            TestContext.Current.CancellationToken);

        db.RecurringExpenses.Add(new RecurringExpenseEntity
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            VendorLabel = "Corner Cafe",
            CategoryId = needs.Id,
            Cadence = ExpenseCadence.Monthly,
            PaymentType = "Card",
            AmountKind = "Fixed",
            FixedAmount = 3.5m,
            State = PatternConfirmationState.Confirmed,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        StatementImportSummary firstImport = await importService.ImportAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(overlappingCsv)),
            "overlap-first.csv",
            userConfirmedAccount: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(firstImport.RequiresAccountConfirmation);
        Assert.Equal(1, firstImport.ImportedCount);
        Assert.Equal(0, firstImport.DedupedCount);

        TransactionEntity importedDebit = await db.Transactions.SingleAsync(
            t => t.MonzoTransactionId == "tx_pipeline_001",
            TestContext.Current.CancellationToken);
        Assert.Equal(needs.Id, importedDebit.CategoryId);

        await attribution.OverrideAttributionAsync(
            importedDebit.Id,
            wants.Id,
            null,
            null,
            "manual",
            TestContext.Current.CancellationToken);

        StatementImportSummary secondImport = await importService.ImportAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(overlappingCsv)),
            "overlap-second.csv",
            userConfirmedAccount: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, secondImport.ImportedCount);
        Assert.Equal(1, secondImport.DedupedCount);
        Assert.Equal(1, await db.Transactions.CountAsync(TestContext.Current.CancellationToken));

        await db.Entry(importedDebit).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(wants.Id, importedDebit.CategoryId);

        AssignmentEntity assignment = await db.Assignments.SingleAsync(
            a => a.TransactionId == importedDebit.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal("manual", assignment.Tag);
    }

    private static ServiceProvider BuildProvider(string ownerKey)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase($"potsure-attribution-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
