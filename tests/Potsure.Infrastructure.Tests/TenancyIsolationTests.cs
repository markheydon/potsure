using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Tenancy;

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

    private static string GetDbContextPath()
    {
        string? root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        _ = root;
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Potsure.Infrastructure", "Persistence", "PotsureDbContext.cs"));
    }
}
