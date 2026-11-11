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

public sealed class BankPotServiceTests
{
    [Fact]
    public async Task CreateAsync_ThreePotsMode_RejectsSecondPotOnSameCategory()
    {
        await using ServiceProvider provider = BuildProvider();
        BankPotService service = provider.GetRequiredService<BankPotService>();
        BudgetProfileSeeder seeder = provider.GetRequiredService<BudgetProfileSeeder>();
        PotsureDbContext db = provider.GetRequiredService<PotsureDbContext>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.Include(p => p.Categories).SingleAsync(TestContext.Current.CancellationToken);
        profile.PotMappingMode = PotMappingMode.ThreePots;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid categoryId = profile.Categories[0].Id;
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
        PotsureDbContext db = provider.GetRequiredService<PotsureDbContext>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.Include(p => p.Categories).SingleAsync(TestContext.Current.CancellationToken);
        profile.PotMappingMode = PotMappingMode.SeveralPots;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid categoryId = profile.Categories[0].Id;
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
        PotsureDbContext db = provider.GetRequiredService<PotsureDbContext>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.Include(p => p.Categories).SingleAsync(TestContext.Current.CancellationToken);
        profile.PotMappingMode = PotMappingMode.SeveralPots;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid categoryId = profile.Categories[0].Id;
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
        PotsureDbContext db = provider.GetRequiredService<PotsureDbContext>();

        await seeder.EnsureProfileAsync(TestContext.Current.CancellationToken);
        BudgetProfileEntity profile = await db.BudgetProfiles.Include(p => p.Categories).SingleAsync(TestContext.Current.CancellationToken);
        profile.PotMappingMode = PotMappingMode.SeveralPots;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid categoryId = profile.Categories[0].Id;
        await service.CreateAsync(new CreateBankPotRequest("Pot", categoryId), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SetPotMappingModeAsync(PotMappingMode.CategoryOnly, TestContext.Current.CancellationToken));
    }

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton<ICurrentOwnerAccessor>(new FixedOwnerAccessor("bank-pot-tests"));
        services.AddPotsureInfrastructureServices();
        services.AddDbContext<PotsureDbContext>(options =>
            options
                .UseInMemoryDatabase($"potsure-bank-pots-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private sealed class FixedOwnerAccessor(string ownerKey) : ICurrentOwnerAccessor
    {
        public string OwnerKey => ownerKey;

        public bool IsAuthenticated => true;
    }
}
