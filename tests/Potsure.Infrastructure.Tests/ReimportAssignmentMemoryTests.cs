using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;
using Potsure.Infrastructure.Services;
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

    private static ServiceProvider BuildProvider(string ownerKey)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options.UseInMemoryDatabase($"potsure-attribution-{Guid.NewGuid()}"));
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
