using Potsure.Data.Documents;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Persistence;

public sealed class PotsureUserStateContext
{
    private readonly IUserDocumentStore documentStore;
    private readonly ICurrentOwnerAccessor ownerAccessor;
    private UserStateDocument document = new();
    private string? etag;
    private bool isNew = true;
    private bool loaded;

    public PotsureUserStateContext(IUserDocumentStore documentStore, ICurrentOwnerAccessor ownerAccessor)
    {
        this.documentStore = documentStore;
        this.ownerAccessor = ownerAccessor;
        Database = new UserStateDatabaseFacade();
    }

    public DocumentSet<BudgetProfileEntity> BudgetProfiles => CreateSet(() => document.BudgetProfiles);

    public DocumentSet<BudgetCategoryEntity> BudgetCategories => CreateSet(() => document.BudgetCategories);

    public DocumentSet<BankPotEntity> BankPots => CreateSet(() => document.BankPots);

    public DocumentSet<MonzoAccountAssociationEntity> MonzoAccounts => CreateSet(() => document.MonzoAccounts);

    public DocumentSet<TransactionEntity> Transactions => CreateSet(() => document.Transactions);

    public DocumentSet<StatementUploadEntity> StatementUploads => CreateSet(() => document.StatementUploads);

    public DocumentSet<IncomeSourceEntity> IncomeSources => CreateSet(() => document.IncomeSources);

    public DocumentSet<RecurringExpenseEntity> RecurringExpenses => CreateSet(() => document.RecurringExpenses);

    public DocumentSet<AssignmentEntity> Assignments => CreateSet(() => document.Assignments);

    private DocumentSet<T> CreateSet<T>(Func<List<T>> listAccessor)
        where T : class =>
        new(listAccessor, EnsureLoadedSynchronously, EnsureLoadedAsync);

    public UserStateDatabaseFacade Database { get; }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (loaded)
        {
            return;
        }

        await LoadFromStoreAsync(cancellationToken);
    }

    public async Task ReloadFromStoreAsync(CancellationToken cancellationToken = default)
    {
        loaded = false;
        await LoadFromStoreAsync(cancellationToken);
    }

    private async Task LoadFromStoreAsync(CancellationToken cancellationToken)
    {
        string userId = ownerAccessor.OwnerKey;
        DocumentLoadResult result = await documentStore.LoadAsync(userId, cancellationToken);
        document = result.Document;
        etag = result.ETag;
        isNew = result.IsNew;
        UserStateGraphHydrator.Hydrate(document);
        loaded = true;
    }

    private void EnsureLoadedSynchronously()
    {
        if (loaded)
        {
            return;
        }

        EnsureLoadedAsync().GetAwaiter().GetResult();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        MergeNestedAggregatesIntoLists();
        DetachNavigationReferencesForPersistence();

        string userId = ownerAccessor.OwnerKey;
        document.UserId = userId;

        string? saveEtag = isNew ? null : etag;
        await documentStore.SaveAsync(userId, document, saveEtag, cancellationToken);

        DocumentLoadResult reloaded = await documentStore.LoadAsync(userId, cancellationToken);
        document = reloaded.Document;
        etag = reloaded.ETag;
        isNew = false;
        UserStateGraphHydrator.Hydrate(document);
        return 1;
    }

    public void ClearAllFinancialData()
    {
        EnsureLoadedSynchronously();
        document.BudgetProfiles.Clear();
        document.BudgetCategories.Clear();
        document.BankPots.Clear();
        document.MonzoAccounts.Clear();
        document.Transactions.Clear();
        document.StatementUploads.Clear();
        document.IncomeSources.Clear();
        document.RecurringExpenses.Clear();
        document.Assignments.Clear();
    }

    private void DetachNavigationReferencesForPersistence()
    {
        foreach (BudgetProfileEntity profile in document.BudgetProfiles)
        {
            profile.Categories = [];
            profile.Pots = [];
            profile.MonzoAccount = null;
        }

        foreach (BudgetCategoryEntity category in document.BudgetCategories)
        {
            category.BudgetProfile = null!;
        }

        foreach (BankPotEntity pot in document.BankPots)
        {
            pot.BudgetProfile = null!;
            pot.Category = null!;
        }

        foreach (TransactionEntity transaction in document.Transactions)
        {
            transaction.UploadBatch = null!;
        }
    }

    private void MergeNestedAggregatesIntoLists()
    {
        foreach (BudgetProfileEntity profile in document.BudgetProfiles)
        {
            foreach (BudgetCategoryEntity category in profile.Categories)
            {
                category.BudgetProfileId = profile.Id;
                category.OwnerKey = profile.OwnerKey;
                if (!document.BudgetCategories.Contains(category))
                {
                    document.BudgetCategories.Add(category);
                }
            }

            foreach (BankPotEntity pot in profile.Pots)
            {
                pot.BudgetProfileId = profile.Id;
                pot.OwnerKey = profile.OwnerKey;
                if (!document.BankPots.Contains(pot))
                {
                    document.BankPots.Add(pot);
                }
            }
        }
    }
}
