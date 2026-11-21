using Potsure.Data.Entities;

namespace Potsure.Data.Documents;

/// <summary>
/// Per-user financial state persisted as <c>tenants/{userId}/state.json</c>.
/// </summary>
public sealed class UserStateDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string UserId { get; set; } = string.Empty;

    public List<BudgetProfileEntity> BudgetProfiles { get; set; } = [];

    public List<BudgetCategoryEntity> BudgetCategories { get; set; } = [];

    public List<BankPotEntity> BankPots { get; set; } = [];

    public List<MonzoAccountAssociationEntity> MonzoAccounts { get; set; } = [];

    public List<TransactionEntity> Transactions { get; set; } = [];

    public List<StatementUploadEntity> StatementUploads { get; set; } = [];

    public List<IncomeSourceEntity> IncomeSources { get; set; } = [];

    public List<RecurringExpenseEntity> RecurringExpenses { get; set; } = [];

    public List<AssignmentEntity> Assignments { get; set; } = [];
}
