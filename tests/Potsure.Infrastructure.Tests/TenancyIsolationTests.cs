using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;

namespace Potsure.Infrastructure.Tests;

public sealed class TenancyIsolationTests
{
    [Fact]
    public void EntityOwnershipValidator_RejectsMismatchedOwner()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            EntityOwnershipValidator.EnsureOwnedBy("owner-a", "owner-b"));
    }

    [Fact]
    public void DbContext_AppliesQueryFilterToAllFinancialEntities()
    {
        string source = File.ReadAllText(GetDbContextPath());
        string[] entityNames =
        [
            "BudgetProfileEntity",
            "BudgetPeriodEntity",
            "BudgetCategoryEntity",
            "BankPotEntity",
            "MonzoAccountAssociationEntity",
            "TransactionEntity",
            "StatementUploadEntity",
            "IncomeSourceEntity",
            "RecurringExpenseEntity",
            "AssignmentEntity",
        ];

        foreach (string entityName in entityNames)
        {
            Assert.Contains($"Entity<{entityName}>", source, StringComparison.Ordinal);
            Assert.Contains("HasQueryFilter", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LocalOnlyOwnerAccessor_UsesStableProfileKey()
    {
        LocalOnlyOwnerAccessor accessor = new();
        Assert.Equal(LocalOnlyOwnerAccessor.LocalOwnerKey, accessor.OwnerKey);
    }

    [Fact]
    public async Task DbContext_QueryFiltersHideOtherOwnersRows()
    {
        const string ownerA = "owner-a";
        const string ownerB = "owner-b";
        string databaseName = $"potsure-tenancy-{Guid.NewGuid()}";

        ServiceProvider providerA = BuildProvider(ownerA, databaseName);
        ServiceProvider providerB = BuildProvider(ownerB, databaseName);

        Guid transactionId;
        await using (AsyncServiceScope seedScope = providerA.CreateAsyncScope())
        {
            PotsureDbContext db = seedScope.ServiceProvider.GetRequiredService<PotsureDbContext>();
            TransactionEntity entity = new()
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerA,
                TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = -1m,
                Currency = "GBP",
                Type = "Card payment",
                Name = "Coffee",
                Kind = TransactionKind.ExpenseDebit,
                DedupeKey = "coffee-isolation",
            };
            transactionId = entity.Id;
            db.Transactions.Add(entity);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (AsyncServiceScope scopeA = providerA.CreateAsyncScope())
        {
            PotsureDbContext db = scopeA.ServiceProvider.GetRequiredService<PotsureDbContext>();
            Assert.Equal(1, await db.Transactions.CountAsync(TestContext.Current.CancellationToken));
            Assert.True(await db.Transactions.AnyAsync(
                t => t.Id == transactionId,
                TestContext.Current.CancellationToken));
        }

        await using (AsyncServiceScope scopeB = providerB.CreateAsyncScope())
        {
            PotsureDbContext db = scopeB.ServiceProvider.GetRequiredService<PotsureDbContext>();
            Assert.Equal(0, await db.Transactions.CountAsync(TestContext.Current.CancellationToken));
        }
    }

    private static ServiceProvider BuildProvider(string ownerKey, string databaseName)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }

    private static string GetDbContextPath()
    {
        string? root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        _ = root;
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Potsure.Data", "DbContext", "PotsureDbContext.cs"));
    }
}
