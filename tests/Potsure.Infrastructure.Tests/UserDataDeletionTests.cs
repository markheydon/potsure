using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Deletion;
using Potsure.Infrastructure.Features.Export;

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
    public async Task DeleteAllFinancialDataAsync_OnSqlServer_AfterSeededProfile_RemovesFinancialRows()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__potsure");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip(
                "Set ConnectionStrings__potsure to run SQL-backed deletion (ExecuteDelete requires relational provider).");
        }

        const string owner = "owner-delete-sql";
        ServiceProvider provider = BuildSqlProvider(owner, connectionString);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);

        UserDataDeletionService deletion = scope.ServiceProvider.GetRequiredService<UserDataDeletionService>();
        bool deleted = await deletion.DeleteAllFinancialDataAsync(TestContext.Current.CancellationToken);

        Assert.True(deleted);
        Assert.Equal(0, await db.BudgetProfiles.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAllFinancialDataAsync_RemovesOnlyCurrentOwnerData()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__potsure");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip(
                "Set ConnectionStrings__potsure to run SQL-backed deletion isolation (ExecuteDelete requires relational provider).");
        }

        const string ownerA = "owner-a";
        const string ownerB = "owner-b";

        ServiceProvider providerA = BuildSqlProvider(ownerA, connectionString);
        ServiceProvider providerB = BuildSqlProvider(ownerB, connectionString);

        await using (AsyncServiceScope seedScope = providerA.CreateAsyncScope())
        {
            PotsureDbContext db = seedScope.ServiceProvider.GetRequiredService<PotsureDbContext>();
            StatementUploadEntity upload = CreateUpload(ownerA);
            db.StatementUploads.Add(upload);
            db.Transactions.Add(new TransactionEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerA,
                TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = -1m,
                Currency = "GBP",
                Type = "Card payment",
                Name = "Coffee",
                Kind = TransactionKind.ExpenseDebit,
                DedupeKey = "a-coffee",
                UploadBatchId = upload.Id,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (AsyncServiceScope seedScope = providerB.CreateAsyncScope())
        {
            PotsureDbContext db = seedScope.ServiceProvider.GetRequiredService<PotsureDbContext>();
            StatementUploadEntity upload = CreateUpload(ownerB);
            db.StatementUploads.Add(upload);
            db.Transactions.Add(new TransactionEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerB,
                TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = -2m,
                Currency = "GBP",
                Type = "Card payment",
                Name = "Tea",
                Kind = TransactionKind.ExpenseDebit,
                DedupeKey = "b-tea",
                UploadBatchId = upload.Id,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (AsyncServiceScope deleteScope = providerA.CreateAsyncScope())
        {
            UserDataDeletionService deletion = deleteScope.ServiceProvider.GetRequiredService<UserDataDeletionService>();
            bool deleted = await deletion.DeleteAllFinancialDataAsync(TestContext.Current.CancellationToken);
            Assert.True(deleted);
        }

        await using (AsyncServiceScope verifyScope = providerB.CreateAsyncScope())
        {
            PotsureDbContext db = verifyScope.ServiceProvider.GetRequiredService<PotsureDbContext>();
            Assert.Equal(1, await db.Transactions.CountAsync(TestContext.Current.CancellationToken));
        }

        await using (AsyncServiceScope cleanupScope = providerA.CreateAsyncScope())
        {
            PotsureDbContext db = cleanupScope.ServiceProvider.GetRequiredService<PotsureDbContext>();
            await db.Transactions.Where(t => t.OwnerKey == ownerA || t.OwnerKey == ownerB)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task DeleteAllFinancialDataAsync_WhenSqlRetryStrategyEnabled_CompletesInTransaction()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__potsure");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Skip(
                "Set ConnectionStrings__potsure to run SQL-backed deletion (ExecuteDelete requires relational provider).");
        }

        const string owner = "owner-delete-retry";
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(owner));
        services.AddSingleton<IExportReauthenticationGate>(new AllowReauthenticationGate());
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        ServiceProvider provider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);

        UserDataDeletionService deletion = scope.ServiceProvider.GetRequiredService<UserDataDeletionService>();
        bool deleted = await deletion.DeleteAllFinancialDataAsync(TestContext.Current.CancellationToken);

        Assert.True(deleted);
    }

    private static ServiceProvider BuildSqlProvider(string ownerKey, string connectionString)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddSingleton<IExportReauthenticationGate>(new AllowReauthenticationGate());
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options.UseSqlServer(connectionString));
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildProvider(
        string ownerKey,
        IExportReauthenticationGate gate,
        string? databaseName = null)
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor(ownerKey));
        services.AddSingleton(gate);
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName ?? $"potsure-deletion-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private static StatementUploadEntity CreateUpload(string ownerKey) =>
        new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = ownerKey,
            FileName = "test.csv",
            Status = StatementUploadStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
        };

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

    private sealed class AllowReauthenticationGate : IExportReauthenticationGate
    {
        public Task<bool> ConfirmAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
