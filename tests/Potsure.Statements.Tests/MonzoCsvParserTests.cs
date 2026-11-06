using Potsure.Budgeting;
using Potsure.Statements;

namespace Potsure.Statements.Tests;

public sealed class MonzoCsvParserTests
{
    [Fact]
    public async Task ParseAsync_GoldenValid_ReturnsEightRowsAndNoErrors()
    {
        await using FileStream stream = OpenFixture("monzo-golden-valid.csv");
        MonzoParseResult result = await MonzoCsvParser.ParseAsync(stream, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
        Assert.Equal(8, result.Rows.Count);
        Assert.Equal(new DateOnly(2026, 10, 1), result.Rows[0].TransactionDate);
        Assert.Equal("tx_anon_001", result.Rows[0].MonzoTransactionId);
        Assert.Equal(14.95m, result.Rows[0].Amount);
        Assert.Equal("GBP", result.Rows[0].Currency);
    }

    [Fact]
    public async Task ParseAsync_GoldenValid_ClassifiesTransactionKinds()
    {
        await using FileStream stream = OpenFixture("monzo-golden-valid.csv");
        MonzoParseResult result = await MonzoCsvParser.ParseAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal(TransactionKind.PotMovement, Classify(result.Rows[0]));
        Assert.Equal(TransactionKind.ExpenseDebit, Classify(result.Rows[1]));
        Assert.Equal(TransactionKind.ExpenseDebit, Classify(result.Rows[2]));
        Assert.Equal(TransactionKind.IncomeCredit, Classify(result.Rows[3]));
        Assert.Equal(TransactionKind.InternalTransfer, Classify(result.Rows[4]));
        Assert.Equal(TransactionKind.InternalTransfer, Classify(result.Rows[5]));
        Assert.Equal(TransactionKind.ExpenseDebit, Classify(result.Rows[6]));
        Assert.Equal(TransactionKind.ExpenseDebit, Classify(result.Rows[7]));
    }

    [Fact]
    public async Task ParseAsync_NonGbpRow_ReportsNonGbpError()
    {
        await using FileStream stream = OpenFixture("monzo-non-gbp-row.csv");
        MonzoParseResult result = await MonzoCsvParser.ParseAsync(stream, TestContext.Current.CancellationToken);

        Assert.Empty(result.Rows);
        MonzoParseError error = Assert.Single(result.Errors);
        Assert.Equal("Non-GBP currency", error.Reason);
        Assert.Equal(2, error.RowNumber);
    }

    [Fact]
    public async Task ParseAsync_NotMonzoLayout_RejectsMissingHeaders()
    {
        await using FileStream stream = OpenFixture("not-monzo-layout.csv");
        MonzoParseResult result = await MonzoCsvParser.ParseAsync(stream, TestContext.Current.CancellationToken);

        Assert.Empty(result.Rows);
        Assert.Contains(result.Errors, e => e.Reason.Contains("Missing required header", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Deduplicate_GoldenFallbackKey_RemovesDuplicateWithoutTransactionId()
    {
        await using FileStream stream = OpenFixture("monzo-golden-dedupe-fallback.csv");
        MonzoParseResult result = await MonzoCsvParser.ParseAsync(stream, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Rows.Count);

        TransactionDeduplicationResult deduped = TransactionDeduplicator.Deduplicate(result.Rows);

        Assert.Single(deduped.UniqueRows);
        Assert.Equal(1, deduped.DuplicateCount);
    }

    [Fact]
    public async Task ImportValidator_NonGbpParsedRow_ReportsError()
    {
        MonzoCsvRow row = new(
            "tx_test",
            new DateOnly(2026, 10, 1),
            null,
            "Card payment",
            "Shop",
            -1m,
            "USD",
            null,
            null,
            null);

        IReadOnlyList<MonzoImportValidationError> errors = MonzoImportValidator.ValidateRows([row]);

        MonzoImportValidationError error = Assert.Single(errors);
        Assert.Equal("Non-GBP currency", error.Reason);
    }

    private static TransactionKind Classify(MonzoCsvRow row) =>
        TransactionClassifier.Classify(row.Type, row.Name, row.Amount);

    private static FileStream OpenFixture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return File.OpenRead(path);
    }
}
