using Potsure.Data.Documents;

namespace Potsure.Infrastructure.Persistence;

public sealed class DocumentSchemaMigrator
{
    public static UserStateDocument MigrateIfNeeded(UserStateDocument document, string expectedUserId)
    {
        if (!string.IsNullOrEmpty(document.UserId)
            && !string.Equals(document.UserId, expectedUserId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Document user id does not match the blob owner.");
        }

        document.UserId = expectedUserId;

        if (document.SchemaVersion < 1)
        {
            document.SchemaVersion = 0;
            document = ApplyVersion0To1(document);
        }

        if (document.SchemaVersion > UserStateDocument.CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported schema version {document.SchemaVersion}.");
        }

        return document;
    }

    private static UserStateDocument ApplyVersion0To1(UserStateDocument document)
    {
        document.SchemaVersion = 1;
        document.BudgetProfiles ??= [];
        document.BudgetCategories ??= [];
        document.BankPots ??= [];
        document.MonzoAccounts ??= [];
        document.Transactions ??= [];
        document.StatementUploads ??= [];
        document.IncomeSources ??= [];
        document.RecurringExpenses ??= [];
        document.Assignments ??= [];
        return document;
    }
}
