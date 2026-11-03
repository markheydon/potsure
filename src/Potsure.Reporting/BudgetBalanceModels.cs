using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Confirmed income pattern used for expected payment dates on the budget balance report (display only; FR-M018 excludes income from commitments).
/// </summary>
public readonly record struct IncomeScheduleInput(
    IncomeCadence Cadence,
    decimal? FixedAmount,
    DateOnly AnchorDate);

/// <summary>
/// An expected income receipt within the selected period (cadence-derived, not assumed four payments per month).
/// </summary>
public readonly record struct ExpectedIncomePayment(
    DateOnly ExpectedDate,
    Money Amount);

/// <summary>
/// Inputs for the budget balance report (commitments from confirmed recurring expenses only).
/// </summary>
public readonly record struct BudgetBalanceReportInput(
    AllocationPreset Preset,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal BudgetedIncomeAmount,
    decimal ActualReceiptsInPeriod,
    IReadOnlyList<CommitmentExpenseInput> ConfirmedExpenseCommitments,
    IReadOnlyList<IncomeScheduleInput> ConfirmedIncomeSchedules);

/// <summary>
/// Budget balance and required income report (FR-M018, FR-M019, FR-M033).
/// </summary>
public readonly record struct BudgetBalanceReport(
    DateRange Period,
    Money BudgetedIncome,
    Money ActualReceiptsInPeriod,
    IReadOnlyList<CategoryCommitmentInput> PeriodCommitments,
    RequiredIncomeResult RequiredIncome,
    IReadOnlyList<ExpectedIncomePayment> ExpectedIncomePayments);
