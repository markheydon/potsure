namespace Potsure.Recurrence.Tests;

public sealed class VariableExpenseEstimatorTests
{
    [Fact]
    public void Estimate_WithLessThanThreeMonthsHistory_ExposesPartialCoverageLabelling()
    {
        DateOnly asOf = new(2026, 3, 15);
        List<(DateOnly Date, decimal Amount)> debits =
        [
            (new(2026, 3, 1), -40m),
            (new(2026, 3, 8), -35m),
        ];

        List<DateOnly> coverage = [];
        for (DateOnly day = new(2026, 3, 1); day <= asOf; day = day.AddDays(1))
        {
            coverage.Add(day);
        }

        VariableExpenseEstimateResult result = VariableExpenseEstimator.Estimate(debits, coverage, asOf);

        Assert.True(result.HasIncompleteMonths);
        Assert.Contains("partial coverage", result.CoverageLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("15/31 days covered", result.CoverageLabel, StringComparison.Ordinal);
    }
}
