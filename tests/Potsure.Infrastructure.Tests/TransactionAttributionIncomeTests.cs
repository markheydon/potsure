using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Tests;

public sealed class TransactionAttributionIncomeTests
{
    [Fact]
    public async Task AttributeMatchingCreditsAsync_LinksMatchingIncomeCredit()
    {
        const string owner = "owner-income-attr";
        await using ServiceProvider provider = BuildProvider(owner);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
        TransactionAttributionService sut = scope.ServiceProvider.GetRequiredService<TransactionAttributionService>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);

        IncomeSourceEntity source = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = owner,
            SourceLabel = "Employer Ltd",
            Cadence = IncomeCadence.Monthly,
            FixedAmount = 2000m,
            SplitNeedsPercent = 50m,
            SplitWantsPercent = 30m,
            SplitSavingsDebtPercent = 20m,
            State = PatternConfirmationState.Confirmed,
        };
        db.IncomeSources.Add(source);

        Guid transactionId = Guid.NewGuid();
        db.Transactions.Add(new TransactionEntity
        {
            Id = transactionId,
            OwnerKey = owner,
            TransactionDate = new DateOnly(2026, 2, 1),
            Amount = 2000m,
            Currency = "GBP",
            Type = "BACS",
            Name = "EMPLOYER LTD",
            Kind = TransactionKind.IncomeCredit,
            UploadBatchId = Guid.NewGuid(),
            DedupeKey = "income-credit",
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        int attributed = await sut.AttributeMatchingCreditsAsync(source.Id, TestContext.Current.CancellationToken);

        Assert.Equal(1, attributed);

        AssignmentEntity assignment = await db.Assignments.SingleAsync(
            a => a.TransactionId == transactionId,
            TestContext.Current.CancellationToken);

        Assert.True(assignment.IsIncomeAttribution);
        Assert.Equal(source.Id, assignment.IncomeSourceId);
        Assert.Null(assignment.CategoryId);

        TransactionEntity transaction = await db.Transactions.SingleAsync(
            t => t.Id == transactionId,
            TestContext.Current.CancellationToken);
        Assert.Null(transaction.CategoryId);
    }

    private static ServiceProvider BuildProvider(string ownerKey)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase($"potsure-income-attr-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
