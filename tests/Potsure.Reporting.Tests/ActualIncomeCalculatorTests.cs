using Potsure.Budgeting;
using Potsure.Reporting;

namespace Potsure.Reporting.Tests;

public sealed class ActualIncomeCalculatorTests
{
    [Fact]
    public void MeanMonthlyReceipts_UsesOnlyAttributedCredits()
    {
        Guid attributed = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid unattributed = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        IReadOnlyList<PeriodTransactionInput> transactions =
        [
            new(attributed, new DateOnly(2026, 1, 15), TransactionKind.IncomeCredit, 3000m, null),
            new(unattributed, new DateOnly(2026, 1, 20), TransactionKind.IncomeCredit, 5000m, null),
        ];

        decimal mean = ActualIncomeCalculator.MeanMonthlyReceipts(
            transactions,
            new DateOnly(2026, 1, 31),
            windowMonths: 3,
            new HashSet<Guid> { attributed });

        Assert.Equal(1000m, mean);
    }

    [Fact]
    public void MeanMonthlyReceipts_NoAttributedCredits_ReturnsZero()
    {
        IReadOnlyList<PeriodTransactionInput> transactions =
        [
            new(Guid.NewGuid(), new DateOnly(2026, 1, 15), TransactionKind.IncomeCredit, 3000m, null),
        ];

        decimal mean = ActualIncomeCalculator.MeanMonthlyReceipts(
            transactions,
            new DateOnly(2026, 1, 31),
            3,
            new HashSet<Guid>());

        Assert.Equal(0m, mean);
    }
}
