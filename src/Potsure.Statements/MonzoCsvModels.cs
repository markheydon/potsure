namespace Potsure.Statements;

public sealed record MonzoCsvRow(
    string? MonzoTransactionId,
    DateOnly TransactionDate,
    TimeOnly? TransactionTime,
    string Type,
    string Name,
    decimal Amount,
    string Currency,
    string? Description,
    decimal? MoneyOut,
    decimal? MoneyIn);

public sealed record MonzoParseError(int RowNumber, string Reason);

public sealed record MonzoParseResult(
    IReadOnlyList<MonzoCsvRow> Rows,
    IReadOnlyList<MonzoParseError> Errors);
