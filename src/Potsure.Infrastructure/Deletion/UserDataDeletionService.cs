using Microsoft.EntityFrameworkCore;
using Potsure.Data;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Features.Export;

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

        await dbContext.Assignments.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Transactions.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);
        await dbContext.StatementUploads.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);
        await dbContext.RecurringExpenses.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);
        await dbContext.IncomeSources.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);
        await dbContext.BankPots.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);
        await dbContext.BudgetCategories.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);
        await dbContext.BudgetProfiles.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);
        await dbContext.MonzoAccounts.Where(e => e.OwnerKey == ownerKey).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
