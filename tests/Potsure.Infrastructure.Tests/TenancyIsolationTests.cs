using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data.Entities;
using Potsure.Data.Persistence;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Tenancy;

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
    public void LocalOnlyOwnerAccessor_UsesStableProfileKey()
    {
        LocalOnlyOwnerAccessor accessor = new();
        Assert.Equal(LocalOnlyOwnerAccessor.LocalOwnerKey, accessor.OwnerKey);
    }

    [Fact]
    public async Task DocumentStore_UserACannotReadUserBState()
    {
        const string ownerA = "owner-a";
        const string ownerB = "owner-b";

        using ServiceProvider providerA = DocumentStoreTestHarness.BuildProvider(ownerA);
        using ServiceProvider providerB = DocumentStoreTestHarness.BuildProvider(ownerB);

        Guid transactionId;
        await using (AsyncServiceScope seedScope = providerA.CreateAsyncScope())
        {
            PotsureUserStateContext db = seedScope.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
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
            PotsureUserStateContext db = scopeA.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
            Assert.Equal(1, await db.Transactions.CountAsync(TestContext.Current.CancellationToken));
            Assert.True(await db.Transactions.AnyAsync(
                t => t.Id == transactionId,
                TestContext.Current.CancellationToken));
        }

        await using (AsyncServiceScope scopeB = providerB.CreateAsyncScope())
        {
            PotsureUserStateContext db = scopeB.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
            Assert.Equal(0, await db.Transactions.CountAsync(TestContext.Current.CancellationToken));
        }
    }
}
