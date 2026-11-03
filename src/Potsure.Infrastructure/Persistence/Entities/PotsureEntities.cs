using Potsure.Budgeting;

namespace Potsure.Infrastructure.Persistence.Entities;

public sealed class BudgetProfileEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public AllocationPreset SelectedPreset { get; set; } = AllocationPreset.FiftyThirtyTwenty;

    public string Currency { get; set; } = "GBP";

    public PotMappingMode PotMappingMode { get; set; } = PotMappingMode.None;

    public int ForecastHorizonMonths { get; set; } = 3;

    public string? RememberedColumnMappingJson { get; set; }

    public Guid BudgetPeriodId { get; set; }

    public BudgetPeriodEntity BudgetPeriod { get; set; } = null!;

    public MonzoAccountAssociationEntity? MonzoAccount { get; set; }

    public Guid? MonzoAccountId { get; set; }

    public List<BudgetCategoryEntity> Categories { get; set; } = [];

    public List<BankPotEntity> Pots { get; set; } = [];
}

public sealed class BudgetPeriodEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal BudgetedIncomeAmount { get; set; }
}

public sealed class BudgetCategoryEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public Guid BudgetProfileId { get; set; }

    public BudgetProfileEntity BudgetProfile { get; set; } = null!;

    public BudgetCategoryKind Kind { get; set; }

    public decimal AllocationAmount { get; set; }

    public decimal RecordedFundingDirectAmount { get; set; }

    public decimal PlannedFutureFundingDirectAmount { get; set; }
}

public sealed class BankPotEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public Guid BudgetProfileId { get; set; }

    public BudgetProfileEntity BudgetProfile { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public BudgetCategoryEntity Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public decimal? AllocationToPotAmount { get; set; }

    public decimal RecordedFundingAmount { get; set; }

    public decimal? PlannedFutureFundingAmount { get; set; }
}

public sealed class MonzoAccountAssociationEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? AccountFingerprint { get; set; }

    public bool ConfirmedByUser { get; set; }
}

public sealed class TransactionEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public string? MonzoTransactionId { get; set; }

    public DateOnly TransactionDate { get; set; }

    public TimeOnly? TransactionTime { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string Type { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TransactionKind Kind { get; set; }

    public Guid UploadBatchId { get; set; }

    public StatementUploadEntity UploadBatch { get; set; } = null!;

    public string DedupeKey { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    public Guid? MappedPotId { get; set; }
}

public sealed class IncomeSourceEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public string SourceLabel { get; set; } = string.Empty;

    public IncomeCadence Cadence { get; set; }

    public decimal? FixedAmount { get; set; }

    public decimal? EstimateAmount { get; set; }

    public decimal SplitNeedsPercent { get; set; }

    public decimal SplitWantsPercent { get; set; }

    public decimal SplitSavingsDebtPercent { get; set; }

    public PatternConfirmationState State { get; set; }
}

public sealed class RecurringExpenseEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public string VendorLabel { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Guid? MappedPotId { get; set; }

    public ExpenseCadence Cadence { get; set; }

    public string PaymentType { get; set; } = "Other";

    public string AmountKind { get; set; } = "Fixed";

    public decimal? FixedAmount { get; set; }

    public decimal? MonthlyEstimate { get; set; }

    public string? EstimateCoverageLabel { get; set; }

    public string? Tag { get; set; }

    public PatternConfirmationState State { get; set; }
}

public sealed class AssignmentEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public Guid? TransactionId { get; set; }

    public Guid? RecurringExpenseId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid? MappedPotId { get; set; }

    public string? Tag { get; set; }

    public DateTimeOffset ConfirmedAt { get; set; }
}

public enum StatementUploadStatus
{
    Pending,
    Completed,
    Failed,
    RolledBack,
}

public sealed class StatementUploadEntity
{
    public Guid Id { get; set; }

    public string OwnerKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public StatementUploadStatus Status { get; set; }

    public int ImportedCount { get; set; }

    public int SkippedCount { get; set; }

    public int DedupedCount { get; set; }

    public int TransferExcludedCount { get; set; }

    public string SkippedReasonsJson { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; }

    public List<TransactionEntity> Transactions { get; set; } = [];
}
