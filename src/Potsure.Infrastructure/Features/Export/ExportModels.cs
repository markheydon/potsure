using Potsure.Budgeting;

namespace Potsure.Infrastructure.Features.Export;

public enum ExportFormat
{
    JsonBundle,
    CsvZip,
}

public sealed record ExportResult(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record UserFinancialExportSnapshot(
    DateTimeOffset ExportedAt,
    AllocationPreset Preset,
    int ForecastHorizonMonths,
    int ActualIncomeWindowMonths,
    decimal? EstimatedIncomeAmount,
    PlanIncomeCadence? EstimatedIncomeCadence,
    PotMappingMode PotMappingMode,
    IReadOnlyList<CategoryExportRow> Categories,
    IReadOnlyList<BankPotExportRow> BankPots,
    MonzoAccountExportRow? MonzoAccount,
    IReadOnlyList<IncomeSourceExportRow> IncomeSources,
    IReadOnlyList<RecurringExpenseExportRow> RecurringExpenses,
    IReadOnlyList<AssignmentExportRow> Assignments,
    IReadOnlyList<TransactionExportRow> Transactions);

public sealed record CategoryExportRow(
    BudgetCategoryKind Kind,
    decimal Allocation,
    decimal RecordedFundingDirect,
    decimal PlannedFutureFundingDirect);

public sealed record BankPotExportRow(
    Guid Id,
    string Name,
    BudgetCategoryKind CategoryKind,
    decimal? AllocationToPot,
    decimal RecordedFunding);

public sealed record MonzoAccountExportRow(string? DisplayName, string? Fingerprint);

public sealed record IncomeSourceExportRow(
    Guid Id,
    string SourceLabel,
    IncomeCadence Cadence,
    decimal? FixedAmount,
    decimal SplitNeedsPercent,
    decimal SplitWantsPercent,
    decimal SplitSavingsDebtPercent,
    string State);

public sealed record RecurringExpenseExportRow(
    Guid Id,
    string VendorLabel,
    BudgetCategoryKind CategoryKind,
    ExpenseCadence Cadence,
    ExpenseAmountKind AmountKind,
    decimal? FixedAmount,
    decimal? StoredMonthlyEstimate,
    DateOnly? PaymentAnchorDate,
    string? Tag,
    string State);

public sealed record AssignmentExportRow(
    Guid Id,
    Guid? TransactionId,
    Guid? RecurringExpenseId,
    Guid? IncomeSourceId,
    bool IsIncomeAttribution,
    BudgetCategoryKind? CategoryKind,
    string? Tag,
    DateTimeOffset ConfirmedAt);

public sealed record TransactionExportRow(
    Guid Id,
    string? MonzoTransactionId,
    DateOnly Date,
    TimeOnly? Time,
    string Type,
    string Name,
    decimal Amount,
    string Currency,
    string? Description,
    TransactionKind Kind,
    BudgetCategoryKind? CategoryKind,
    string? MappedPotName,
    string? Tag);
