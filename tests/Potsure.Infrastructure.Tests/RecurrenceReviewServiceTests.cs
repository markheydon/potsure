using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Features.Recurrence;

namespace Potsure.Infrastructure.Tests;

/// <summary>
/// FR-M008: rejected patterns must re-appear as suggestions when detection matches again.
/// </summary>
public sealed class RecurrenceReviewServiceTests
{
    [Fact]
    public async Task RejectedExpenseWithFreshDetection_AppearsAsSuggested()
    {
        const string owner = "test-owner";
        ServiceProvider provider = BuildProvider(owner);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        RecurrenceReviewService review = scope.ServiceProvider.GetRequiredService<RecurrenceReviewService>();

        BudgetCategoryEntity needs = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            Kind = BudgetCategoryKind.Needs,
        };
        db.BudgetCategories.Add(needs);

        RecurringExpenseEntity rejected = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            VendorLabel = "Coffee Shop",
            CategoryId = needs.Id,
            Cadence = ExpenseCadence.Weekly,
            PaymentType = "Card",
            AmountKind = ExpenseAmountKind.Fixed,
            FixedAmount = 10m,
            State = PatternConfirmationState.Rejected,
        };
        db.RecurringExpenses.Add(rejected);

        DateOnly start = new(2026, 1, 1);
        Guid uploadId = Guid.NewGuid();
        db.StatementUploads.Add(new StatementUploadEntity
        {
            Id = uploadId,
            OwnerKey = owner,
            FileName = "test.csv",
            Status = StatementUploadStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        for (int week = 0; week < 3; week++)
        {
            db.Transactions.Add(new TransactionEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = owner,
                TransactionDate = start.AddDays(week * 7),
                Amount = -10m,
                Currency = "GBP",
                Type = "Card payment",
                Name = "Coffee Shop",
                Kind = TransactionKind.ExpenseDebit,
                UploadBatchId = uploadId,
                DedupeKey = $"coffee-{week}",
            });
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IReadOnlyList<RecurrenceReviewItem> items = await review.GetReviewItemsAsync(TestContext.Current.CancellationToken);
        RecurrenceReviewItem? coffee = items.SingleOrDefault(i => i.Label == "Coffee Shop");

        Assert.NotNull(coffee);
        Assert.Equal(PatternConfirmationState.Suggested, coffee.State);
        Assert.Equal(rejected.Id, coffee.PersistedId);
    }

    private static ServiceProvider BuildProvider(string ownerKey)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase($"potsure-recurrence-review-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
