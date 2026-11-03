using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Deletion;
using Potsure.Infrastructure.Export;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Tests;

/// <summary>
/// FR-M023: permanent deletion of all financial data for the current owner.
/// </summary>
public sealed class UserDataDeletionTests
{
    [Fact]
    public async Task DeleteAllFinancialDataAsync_WhenReauthenticationDeclined_ReturnsFalseWithoutDeleting()
    {
        const string owner = "owner-a";
        ServiceProvider provider = BuildProvider(owner, new DenyReauthenticationGate());

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);

        UserDataDeletionService deletion = scope.ServiceProvider.GetRequiredService<UserDataDeletionService>();
        bool deleted = await deletion.DeleteAllFinancialDataAsync(TestContext.Current.CancellationToken);

        Assert.False(deleted);
        Assert.Equal(1, await db.BudgetProfiles.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void DeleteAllFinancialDataAsync_ExecutesDeleteOnEveryFinancialDbSet()
    {
        string deletionSource = File.ReadAllText(GetDeletionServicePath());
        string dbContextSource = File.ReadAllText(GetDbContextPath());
        string[] dbSetNames =
        [
            nameof(PotsureDbContext.Assignments),
            nameof(PotsureDbContext.Transactions),
            nameof(PotsureDbContext.StatementUploads),
            nameof(PotsureDbContext.RecurringExpenses),
            nameof(PotsureDbContext.IncomeSources),
            nameof(PotsureDbContext.BankPots),
            nameof(PotsureDbContext.BudgetCategories),
            nameof(PotsureDbContext.BudgetProfiles),
            nameof(PotsureDbContext.BudgetPeriods),
            nameof(PotsureDbContext.MonzoAccounts),
        ];

        foreach (string dbSetName in dbSetNames)
        {
            Assert.Contains($"dbContext.{dbSetName}", deletionSource, StringComparison.Ordinal);
            Assert.Contains(dbSetName, dbContextSource, StringComparison.Ordinal);
            Assert.Contains("HasQueryFilter", dbContextSource, StringComparison.Ordinal);
        }
    }

    private static ServiceProvider BuildProvider(string ownerKey, IExportReauthenticationGate gate)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddSingleton(gate);
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase($"potsure-deletion-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private static string GetDeletionServicePath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Potsure.Infrastructure", "Deletion", "UserDataDeletionService.cs"));

    private static string GetDbContextPath() =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Potsure.Infrastructure", "Persistence", "PotsureDbContext.cs"));

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }

    private sealed class DenyReauthenticationGate : IExportReauthenticationGate
    {
        public Task<bool> ConfirmAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
