using Microsoft.EntityFrameworkCore;
using Potsure.Data;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Export;

namespace Potsure.Infrastructure.Deletion;

/// <summary>
/// Permanently deletes all financial data for the current owner (FR-M023).
/// </summary>
public sealed class UserDataDeletionService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor,
    IExportReauthenticationGate reauthenticationGate)
{
    public async Task<bool> DeleteAllFinancialDataAsync(CancellationToken cancellationToken = default)
    {
        if (!await reauthenticationGate.ConfirmAsync(cancellationToken))
        {
            return false;
        }

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        string ownerKey = ownerAccessor.OwnerKey;

        await dbContext.Assignments.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Transactions.ExecuteDeleteAsync(cancellationToken);
        await dbContext.StatementUploads.ExecuteDeleteAsync(cancellationToken);
        await dbContext.RecurringExpenses.ExecuteDeleteAsync(cancellationToken);
        await dbContext.IncomeSources.ExecuteDeleteAsync(cancellationToken);
        await dbContext.BankPots.ExecuteDeleteAsync(cancellationToken);
        await dbContext.BudgetCategories.ExecuteDeleteAsync(cancellationToken);
        await dbContext.BudgetProfiles.ExecuteDeleteAsync(cancellationToken);
        await dbContext.BudgetPeriods.ExecuteDeleteAsync(cancellationToken);
        await dbContext.MonzoAccounts.ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        _ = ownerKey;
        return true;
    }
}
