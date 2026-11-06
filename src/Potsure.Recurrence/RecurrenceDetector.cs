using Potsure.Budgeting;

namespace Potsure.Recurrence;

/// <summary>
/// Detects recurring income and expense cadences from normalised transactions (FR-M007).
/// </summary>
public static class RecurrenceDetector
{
    private const int MinimumObservations = 3;
    private const decimal FixedAmountTolerance = 0.05m;

    /// <summary>
    /// Returns whether the transaction kind must be excluded from detection input.
    /// </summary>
    public static bool ExcludesKind(TransactionKind kind) =>
        kind is TransactionKind.InternalTransfer or TransactionKind.PotMovement;

    /// <summary>
    /// Groups eligible transactions and infers weekly, fortnightly, or monthly cadences where evidence exists.
    /// </summary>
    public static IReadOnlyList<DetectedRecurrencePattern> Detect(
        IReadOnlyList<RecurrenceTransactionInput> transactions)
    {
        IEnumerable<RecurrenceTransactionInput> eligible = transactions
            .Where(t => !ExcludesKind(t.Kind))
            .Where(t => t.Kind is TransactionKind.ExpenseDebit or TransactionKind.IncomeCredit or TransactionKind.RefundCredit);

        List<DetectedRecurrencePattern> patterns = new();
        foreach (IGrouping<string, RecurrenceTransactionInput> group in eligible
                     .Select(t => t)
                     .Where(t => !string.IsNullOrEmpty(PatternLabelNormalizer.Normalize(t.Label)))
                     .GroupBy(t => PatternLabelNormalizer.Normalize(t.Label)))
        {
            List<RecurrenceTransactionInput> rows = group.OrderBy(t => t.Date).ToList();
            string displayLabel = string.IsNullOrWhiteSpace(rows[^1].Label)
                ? group.Key
                : rows[^1].Label.Trim();
            if (rows.Count < MinimumObservations)
            {
                continue;
            }

            IReadOnlyList<DateOnly> dates = rows.Select(r => r.Date).ToList();
            if (!TryDetectCadence(dates, out int intervalDays))
            {
                continue;
            }

            bool isIncome = rows.All(r => r.Kind == TransactionKind.IncomeCredit);
            if (!isIncome && rows.Any(r => r.Kind == TransactionKind.IncomeCredit))
            {
                continue;
            }

            DetectedAmountBehaviour amountBehaviour = InferAmountBehaviour(rows);
            decimal? typicalAmount = amountBehaviour == DetectedAmountBehaviour.Fixed
                ? MedianAbsoluteAmount(rows)
                : null;

            if (intervalDays is >= 6 and <= 8)
            {
                patterns.Add(CreatePattern(displayLabel, isIncome, weekly: true, fortnightly: false, monthly: false,
                    amountBehaviour, typicalAmount, dates));
            }
            else if (intervalDays is >= 13 and <= 16)
            {
                patterns.Add(CreatePattern(displayLabel, isIncome, weekly: false, fortnightly: true, monthly: false,
                    amountBehaviour, typicalAmount, dates));
            }
            else if (intervalDays is >= 27 and <= 35)
            {
                patterns.Add(CreatePattern(displayLabel, isIncome, weekly: false, fortnightly: false, monthly: true,
                    amountBehaviour, typicalAmount, dates));
            }
        }

        return patterns;
    }

    private static DetectedRecurrencePattern CreatePattern(
        string label,
        bool isIncome,
        bool weekly,
        bool fortnightly,
        bool monthly,
        DetectedAmountBehaviour amountBehaviour,
        decimal? typicalAmount,
        IReadOnlyList<DateOnly> dates)
    {
        IncomeCadence? incomeCadence = isIncome
            ? weekly ? IncomeCadence.Weekly
            : fortnightly ? IncomeCadence.Fortnightly
            : monthly ? IncomeCadence.Monthly
            : null
            : null;

        ExpenseCadence? expenseCadence = !isIncome
            ? weekly ? ExpenseCadence.Weekly
            : fortnightly ? ExpenseCadence.Fortnightly
            : monthly ? ExpenseCadence.Monthly
            : null
            : null;

        return new DetectedRecurrencePattern(
            label,
            isIncome,
            incomeCadence,
            expenseCadence,
            amountBehaviour,
            typicalAmount,
            dates);
    }

    private static bool TryDetectCadence(IReadOnlyList<DateOnly> dates, out int intervalDays)
    {
        intervalDays = 0;
        if (dates.Count < MinimumObservations)
        {
            return false;
        }

        List<int> gaps = new(dates.Count - 1);
        for (int i = 1; i < dates.Count; i++)
        {
            gaps.Add(dates[i].DayNumber - dates[i - 1].DayNumber);
        }

        gaps.Sort();
        int medianGap = gaps[gaps.Count / 2];
        int withinTolerance = gaps.Count(g => Math.Abs(g - medianGap) <= 2);
        if (withinTolerance < gaps.Count - 1)
        {
            return false;
        }

        intervalDays = medianGap;
        return intervalDays is (>= 6 and <= 8) or (>= 13 and <= 16) or (>= 27 and <= 35);
    }

    private static DetectedAmountBehaviour InferAmountBehaviour(IReadOnlyList<RecurrenceTransactionInput> rows)
    {
        decimal[] amounts = rows
            .Select(r => Math.Abs(r.SignedAmount))
            .Where(a => a > 0m)
            .ToArray();

        if (amounts.Length == 0)
        {
            return DetectedAmountBehaviour.Unknown;
        }

        decimal median = Median(amounts);
        if (median == 0m)
        {
            return DetectedAmountBehaviour.Unknown;
        }

        bool allWithinTolerance = amounts.All(a => Math.Abs(a - median) / median <= FixedAmountTolerance);
        return allWithinTolerance ? DetectedAmountBehaviour.Fixed : DetectedAmountBehaviour.Variable;
    }

    private static decimal MedianAbsoluteAmount(IReadOnlyList<RecurrenceTransactionInput> rows)
    {
        decimal[] amounts = rows.Select(r => Math.Abs(r.SignedAmount)).Where(a => a > 0m).ToArray();
        return amounts.Length == 0 ? 0m : Median(amounts);
    }

    private static decimal Median(decimal[] values)
    {
        Array.Sort(values);
        int mid = values.Length / 2;
        return values.Length % 2 == 1
            ? values[mid]
            : (values[mid - 1] + values[mid]) / 2m;
    }
}
