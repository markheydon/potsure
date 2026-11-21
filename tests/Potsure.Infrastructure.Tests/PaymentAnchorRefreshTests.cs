using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data.Entities;
using Potsure.Data.Persistence;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Features.Recurrence;
using Potsure.Infrastructure.Features.Reports;

namespace Potsure.Infrastructure.Tests;

public sealed class PaymentAnchorRefreshTests
{
    [Fact]
    public void ResolveEffectiveExpenseAnchor_WhenNotOverridden_UsesLatestOfStoredAndHistory()
    {
        RecurringExpenseEntity expense = new()
        {
            PaymentAnchorDate = new DateOnly(2026, 1, 15),
            PaymentAnchorUserOverride = false,
        };

        DateOnly? effective = CommitmentPaymentAnchorResolver.ResolveEffectiveExpenseAnchor(
            expense,
            new DateOnly(2026, 3, 10));

        Assert.Equal(new DateOnly(2026, 3, 10), effective);
    }

    [Fact]
    public void ResolveEffectiveExpenseAnchor_WhenOverridden_IgnoresHistory()
    {
        RecurringExpenseEntity expense = new()
        {
            PaymentAnchorDate = new DateOnly(2026, 1, 15),
            PaymentAnchorUserOverride = true,
        };

        DateOnly? effective = CommitmentPaymentAnchorResolver.ResolveEffectiveExpenseAnchor(
            expense,
            new DateOnly(2026, 3, 10));

        Assert.Equal(new DateOnly(2026, 1, 15), effective);
    }

    [Fact]
    public void ResolveEffectiveExpenseAnchor_WhenNoHistoryOrStored_ReturnsNull()
    {
        RecurringExpenseEntity expense = new()
        {
            PaymentAnchorDate = null,
            PaymentAnchorUserOverride = false,
        };

        DateOnly? effective = CommitmentPaymentAnchorResolver.ResolveEffectiveExpenseAnchor(expense, null);

        Assert.Null(effective);
    }

    [Fact]
    public async Task RefreshNonOverriddenPaymentAnchorsAfterImport_AdvancesStoredAnchor()
    {
        const string owner = "anchor-owner";
        ServiceProvider provider = BuildProvider(owner);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        PotsureUserStateContext db = scope.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
        ExpensePatternService expenseService = scope.ServiceProvider.GetRequiredService<ExpensePatternService>();

        BudgetCategoryEntity category = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            Kind = BudgetCategoryKind.Needs,
        };
        db.BudgetCategories.Add(category);

        Guid uploadId = Guid.NewGuid();
        db.StatementUploads.Add(new StatementUploadEntity
        {
            Id = uploadId,
            OwnerKey = owner,
            FileName = "test.csv",
            Status = StatementUploadStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        RecurringExpenseEntity expense = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            VendorLabel = "Power Co",
            CategoryId = category.Id,
            Cadence = ExpenseCadence.Monthly,
            PaymentType = "Card",
            AmountKind = ExpenseAmountKind.Fixed,
            FixedAmount = 80m,
            PaymentAnchorDate = new DateOnly(2026, 1, 5),
            PaymentAnchorUserOverride = false,
            State = PatternConfirmationState.Confirmed,
        };
        db.RecurringExpenses.Add(expense);

        db.Transactions.Add(new TransactionEntity
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            TransactionDate = new DateOnly(2026, 3, 12),
            Amount = -80m,
            Currency = "GBP",
            Type = "Card payment",
            Name = "Power Co",
            Kind = TransactionKind.ExpenseDebit,
            UploadBatchId = uploadId,
            DedupeKey = "power-mar",
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        int updated = await expenseService.RefreshNonOverriddenPaymentAnchorsAfterImportAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, updated);
        RecurringExpenseEntity reloaded = await db.RecurringExpenses.SingleAsync(
            e => e.Id == expense.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(new DateOnly(2026, 3, 12), reloaded.PaymentAnchorDate);
    }

    private static ServiceProvider BuildProvider(string ownerKey) =>
        DocumentStoreTestHarness.BuildProvider(ownerKey);
}
