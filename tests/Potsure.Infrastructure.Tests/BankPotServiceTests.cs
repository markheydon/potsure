using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Data.Entities;
using Potsure.Data.Persistence;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Tests;

public sealed class BankPotServiceTests
{
    [Fact]
    public async Task CreateAsync_ThreePotsMode_RejectsSecondPotOnSameCategory()
    {
        await using ServiceProvider provider = BuildProvider();
        BankPotService service = provider.GetRequiredService<BankPotService>();
        BudgetProfileSeeder seeder = provider.GetRequiredService<BudgetProfileSeeder>();
        PotsureUserStateContext db = provider.GetRequiredService<PotsureUserStateContext>();

        await seeder.EnsureProfileAsync(cancellationToken: TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.SingleAsync(TestContext.Current.CancellationToken);
        profile.PotMappingMode = PotMappingMode.ThreePots;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid categoryId = (await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Needs,
            TestContext.Current.CancellationToken)).Id;
        await service.CreateAsync(new CreateBankPotRequest("Needs pot", categoryId), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new CreateBankPotRequest("Duplicate", categoryId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteAsync_RollsRecordedFundingIntoCategory_WhenRequested()
    {
        await using ServiceProvider provider = BuildProvider();
        BankPotService service = provider.GetRequiredService<BankPotService>();
        BudgetProfileSeeder seeder = provider.GetRequiredService<BudgetProfileSeeder>();
        PotsureUserStateContext db = provider.GetRequiredService<PotsureUserStateContext>();

        await seeder.EnsureProfileAsync(cancellationToken: TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.SingleAsync(TestContext.Current.CancellationToken);
        profile.PotMappingMode = PotMappingMode.SeveralPots;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid categoryId = (await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Needs,
            TestContext.Current.CancellationToken)).Id;
        BankPotReadModel pot = await service.CreateAsync(new CreateBankPotRequest("Savings pot", categoryId), TestContext.Current.CancellationToken);

        BankPotEntity entity = await db.BankPots.SingleAsync(p => p.Id == pot.Id, TestContext.Current.CancellationToken);
        entity.RecordedFundingAmount = 40m;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await service.DeleteAsync(
            new DeleteBankPotRequest(pot.Id, BankPotFundingDisposition.RollToCategory),
            TestContext.Current.CancellationToken);

        BudgetCategoryEntity category = await db.BudgetCategories.SingleAsync(c => c.Id == categoryId, TestContext.Current.CancellationToken);
        Assert.Equal(40m, category.RecordedFundingDirectAmount);
        Assert.Equal(0, await db.BankPots.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListAsync_AfterDelete_DoesNotReturnDeletedPot_InSameScope()
    {
        await using ServiceProvider provider = BuildProvider();
        BankPotService service = provider.GetRequiredService<BankPotService>();
        BudgetProfileSeeder seeder = provider.GetRequiredService<BudgetProfileSeeder>();
        PotsureUserStateContext db = provider.GetRequiredService<PotsureUserStateContext>();

        await seeder.EnsureProfileAsync(cancellationToken: TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.SingleAsync(TestContext.Current.CancellationToken);
        profile.PotMappingMode = PotMappingMode.SeveralPots;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid categoryId = (await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Needs,
            TestContext.Current.CancellationToken)).Id;
        BankPotReadModel pot = await service.CreateAsync(new CreateBankPotRequest("Pot", categoryId), TestContext.Current.CancellationToken);

        Assert.Single(await service.ListAsync(TestContext.Current.CancellationToken));

        await service.DeleteAsync(
            new DeleteBankPotRequest(pot.Id),
            TestContext.Current.CancellationToken);

        Assert.Empty(await service.ListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetPotMappingModeAsync_BlocksCategoryOnly_WhenPotsExist()
    {
        await using ServiceProvider provider = BuildProvider();
        BankPotService service = provider.GetRequiredService<BankPotService>();
        BudgetProfileSeeder seeder = provider.GetRequiredService<BudgetProfileSeeder>();
        PotsureUserStateContext db = provider.GetRequiredService<PotsureUserStateContext>();

        await seeder.EnsureProfileAsync(cancellationToken: TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.SingleAsync(TestContext.Current.CancellationToken);
        profile.PotMappingMode = PotMappingMode.SeveralPots;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid categoryId = (await db.BudgetCategories.SingleAsync(
            c => c.Kind == BudgetCategoryKind.Needs,
            TestContext.Current.CancellationToken)).Id;
        await service.CreateAsync(new CreateBankPotRequest("Pot", categoryId), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SetPotMappingModeAsync(PotMappingMode.CategoryOnly, TestContext.Current.CancellationToken));
    }

    private static ServiceProvider BuildProvider() =>
        DocumentStoreTestHarness.BuildProvider("bank-pot-tests");
}
