using Microsoft.Extensions.Options;
using Potsure.Data.Documents;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Tests;

public sealed class UserDocumentStoreTests
{
    [Fact]
    public async Task Save_IfMatchFailure_WhenEtagStale()
    {
        string root = Path.Combine(Path.GetTempPath(), "potsure-store-tests", Guid.NewGuid().ToString());
        FileUserDocumentStore store = new(Options.Create(new DocumentStoreOptions { DataDirectory = root }));
        const string userId = "user-a";

        DocumentLoadResult first = await store.LoadAsync(userId, TestContext.Current.CancellationToken);
        first.Document.IncomeSources.Add(new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = userId,
            SourceLabel = "Salary",
            Cadence = Potsure.Budgeting.IncomeCadence.Monthly,
            State = Potsure.Budgeting.PatternConfirmationState.Confirmed,
        });
        await store.SaveAsync(userId, first.Document, first.ETag, TestContext.Current.CancellationToken);

        DocumentLoadResult second = await store.LoadAsync(userId, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<DocumentStoreConcurrencyException>(() =>
            store.SaveAsync(userId, second.Document, first.ETag, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Load_UpgradesSchemaVersion0To1()
    {
        string root = Path.Combine(Path.GetTempPath(), "potsure-store-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "tenants", "user-b", "state.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(
            path,
            """{"userId":"user-b","budgetProfiles":[],"transactions":[]}""",
            TestContext.Current.CancellationToken);

        FileUserDocumentStore store = new(Options.Create(new DocumentStoreOptions { DataDirectory = root }));

        DocumentLoadResult result = await store.LoadAsync("user-b", TestContext.Current.CancellationToken);
        Assert.Equal(1, result.Document.SchemaVersion);
        Assert.NotNull(result.ETag);
    }
}
