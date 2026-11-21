using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data.Documents;
using Potsure.Data.Entities;
using Potsure.Data.Identity;
using Potsure.Data.Persistence;
using Potsure.Infrastructure.Deletion;
using Potsure.Infrastructure.Features.Export;
using Potsure.Infrastructure.Identity;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Repositories;

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
        ServiceProvider provider = DocumentStoreTestHarness.BuildProvider(
            owner,
            reauthenticationGate: new DenyReauthenticationGate());

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        PotsureUserStateContext db = scope.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
        await seeder.EnsureProfileAsync(cancellationToken: TestContext.Current.CancellationToken);

        UserDataDeletionService deletion = scope.ServiceProvider.GetRequiredService<UserDataDeletionService>();
        bool deleted = await deletion.DeleteAllFinancialDataAsync(TestContext.Current.CancellationToken);

        Assert.False(deleted);
        Assert.Equal(1, await db.BudgetProfiles.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAllFinancialDataAsync_RemovesStateBlobForOwner()
    {
        const string owner = "owner-delete";
        string root = Path.Combine(Path.GetTempPath(), "potsure-delete", Guid.NewGuid().ToString());
        ServiceProvider provider = DocumentStoreTestHarness.BuildProvider(
            owner,
            dataDirectory: root,
            reauthenticationGate: new AllowReauthenticationGate());

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
            await seeder.EnsureProfileAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        await using (AsyncServiceScope deleteScope = provider.CreateAsyncScope())
        {
            UserDataDeletionService deletion = deleteScope.ServiceProvider.GetRequiredService<UserDataDeletionService>();
            bool deleted = await deletion.DeleteAllFinancialDataAsync(TestContext.Current.CancellationToken);
            Assert.True(deleted);
        }

        string statePath = Path.Combine(root, "tenants", owner, "state.json");
        Assert.False(File.Exists(statePath));
    }

    [Fact]
    public async Task DeleteHostedUserBlobsAsync_RemovesStateIdentityAndEmailIndex()
    {
        const string userId = "hosted-user-delete";
        const string email = "delete-me@example.com";
        string root = Path.Combine(Path.GetTempPath(), "potsure-hosted-delete", Guid.NewGuid().ToString());

        ServiceProvider provider = DocumentStoreTestHarness.BuildProvider(
            userId,
            dataDirectory: root,
            reauthenticationGate: new AllowReauthenticationGate());

        await using (AsyncServiceScope seedScope = provider.CreateAsyncScope())
        {
            PotsureUserStateContext db = seedScope.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
            BudgetProfileSeeder seeder = seedScope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
            await seeder.EnsureProfileAsync(cancellationToken: TestContext.Current.CancellationToken);

            IIdentityDocumentStore identity = seedScope.ServiceProvider.GetRequiredService<IIdentityDocumentStore>();
            await identity.SaveUserAsync(
                new IdentityUserDocument
                {
                    Id = userId,
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                },
                ifMatchEtag: null,
                TestContext.Current.CancellationToken);
            string emailHash = IdentityEmailHasher.HashEmailForIndex(email);
            await identity.SaveEmailIndexAsync(
                emailHash,
                new EmailIndexDocument { UserId = userId },
                ifMatchEtag: null,
                TestContext.Current.CancellationToken);
        }

        await using (AsyncServiceScope deleteScope = provider.CreateAsyncScope())
        {
            UserDataDeletionService deletion = deleteScope.ServiceProvider.GetRequiredService<UserDataDeletionService>();
            await deletion.DeleteHostedUserBlobsAsync(
                new ApplicationUserIdentityDeletionRequest(userId, email),
                TestContext.Current.CancellationToken);
        }

        string statePath = Path.Combine(root, "tenants", userId, "state.json");
        string identityPath = Path.Combine(root, UserDocumentPathValidator.IdentityUserBlobName(userId));
        string emailIndexPath = Path.Combine(
            root,
            UserDocumentPathValidator.EmailIndexBlobName(IdentityEmailHasher.HashEmailForIndex(email)));

        Assert.False(File.Exists(statePath));
        Assert.False(File.Exists(identityPath));
        Assert.False(File.Exists(emailIndexPath));
    }

    [Fact]
    public async Task DeleteAllFinancialDataAsync_RemovesOnlyCurrentOwnerData()
    {
        const string ownerA = "owner-a";
        const string ownerB = "owner-b";
        string root = Path.Combine(Path.GetTempPath(), "potsure-delete-iso", Guid.NewGuid().ToString());

        using ServiceProvider providerA = DocumentStoreTestHarness.BuildProvider(
            ownerA,
            dataDirectory: Path.Combine(root, "a"),
            reauthenticationGate: new AllowReauthenticationGate());
        using ServiceProvider providerB = DocumentStoreTestHarness.BuildProvider(
            ownerB,
            dataDirectory: Path.Combine(root, "b"),
            reauthenticationGate: new AllowReauthenticationGate());

        await using (AsyncServiceScope seedScope = providerA.CreateAsyncScope())
        {
            PotsureUserStateContext db = seedScope.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
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

        await using (AsyncServiceScope deleteScope = providerA.CreateAsyncScope())
        {
            UserDataDeletionService deletion = deleteScope.ServiceProvider.GetRequiredService<UserDataDeletionService>();
            bool deleted = await deletion.DeleteAllFinancialDataAsync(TestContext.Current.CancellationToken);
            Assert.True(deleted);
        }

        await using (AsyncServiceScope verifyScope = providerB.CreateAsyncScope())
        {
            PotsureUserStateContext db = verifyScope.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
            Assert.Equal(0, await db.Transactions.CountAsync(TestContext.Current.CancellationToken));
        }

        await using (AsyncServiceScope seedScope = providerB.CreateAsyncScope())
        {
            PotsureUserStateContext db = seedScope.ServiceProvider.GetRequiredService<PotsureUserStateContext>();
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
            Assert.Equal(1, await db.Transactions.CountAsync(TestContext.Current.CancellationToken));
        }
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
