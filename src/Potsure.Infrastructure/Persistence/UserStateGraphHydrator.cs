using Potsure.Data.Documents;
using Potsure.Data.Entities;

namespace Potsure.Infrastructure.Persistence;

public static class UserStateGraphHydrator
{
    public static void Hydrate(UserStateDocument document)
    {
        Dictionary<Guid, BudgetCategoryEntity> categoriesById = document.BudgetCategories.ToDictionary(c => c.Id);
        Dictionary<Guid, StatementUploadEntity> uploadsById = document.StatementUploads.ToDictionary(u => u.Id);
        Dictionary<Guid, MonzoAccountAssociationEntity> monzoById = document.MonzoAccounts.ToDictionary(m => m.Id);

        foreach (BudgetProfileEntity profile in document.BudgetProfiles)
        {
            profile.Categories = document.BudgetCategories
                .Where(c => c.BudgetProfileId == profile.Id)
                .ToList();
            profile.Pots = document.BankPots
                .Where(p => p.BudgetProfileId == profile.Id)
                .ToList();
            profile.MonzoAccount = profile.MonzoAccountId is Guid monzoId && monzoById.TryGetValue(monzoId, out MonzoAccountAssociationEntity? monzo)
                ? monzo
                : null;

            foreach (BudgetCategoryEntity category in profile.Categories)
            {
                category.BudgetProfile = profile;
            }

            foreach (BankPotEntity pot in profile.Pots)
            {
                pot.BudgetProfile = profile;
                if (categoriesById.TryGetValue(pot.CategoryId, out BudgetCategoryEntity? category))
                {
                    pot.Category = category;
                }
            }
        }

        foreach (BankPotEntity pot in document.BankPots)
        {
            if (categoriesById.TryGetValue(pot.CategoryId, out BudgetCategoryEntity? category))
            {
                pot.Category = category;
            }
        }

        foreach (TransactionEntity transaction in document.Transactions)
        {
            if (uploadsById.TryGetValue(transaction.UploadBatchId, out StatementUploadEntity? upload))
            {
                transaction.UploadBatch = upload;
            }
        }
    }
}
