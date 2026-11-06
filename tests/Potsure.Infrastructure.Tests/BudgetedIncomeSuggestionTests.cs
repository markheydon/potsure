using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Tests;

public sealed class BudgetedIncomeSuggestionTests
{
    [Fact]
    public async Task GetSuggestionAsync_MonthlyProjectsExpectedPaymentsIntoPeriod()
    {
        const string owner = "owner-suggest";
        await using ServiceProvider provider = BuildProvider(owner);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
        BudgetedIncomeSuggestionService sut = scope.ServiceProvider.GetRequiredService<BudgetedIncomeSuggestionService>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.Include(p => p.BudgetPeriod).SingleAsync(
            TestContext.Current.CancellationToken);
        profile.BudgetPeriod.StartDate = new DateOnly(2026, 1, 1);
        profile.BudgetPeriod.EndDate = new DateOnly(2026, 3, 31);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IncomeSourceEntity monthly = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            SourceLabel = "Employer Ltd",
            Cadence = IncomeCadence.Monthly,
            FixedAmount = 1000m,
            EstimateAmount = null,
            SplitNeedsPercent = 50m,
            SplitWantsPercent = 30m,
            SplitSavingsDebtPercent = 20m,
            State = PatternConfirmationState.Confirmed,
        };
        db.IncomeSources.Add(monthly);

        Guid batchId = Guid.NewGuid();
        db.StatementUploads.Add(new StatementUploadEntity
        {
            Id = batchId,
            OwnerKey = owner,
            FileName = "test.csv",
            Status = StatementUploadStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        db.Transactions.Add(new TransactionEntity
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            TransactionDate = new DateOnly(2026, 1, 15),
            Amount = 1000m,
            Currency = "GBP",
            Type = "BACS",
            Name = "Employer Ltd",
            Kind = TransactionKind.IncomeCredit,
            UploadBatchId = batchId,
            DedupeKey = "income-1",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        BudgetedIncomeSuggestion? result = await sut.GetSuggestionAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(3000m, result!.SuggestedAmount);
        Assert.Contains("01/01/2026", result.Rationale);
        Assert.Contains("31/03/2026", result.Rationale);
    }

    private static ServiceProvider BuildProvider(string ownerKey)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase($"potsure-suggest-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
