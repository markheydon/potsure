using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Potsure.Statements;

public static class MonzoCsvParser
{
    private static readonly string[] RequiredHeaders =
    [
        "Date",
        "Type",
        "Name",
        "Amount",
        "Currency",
    ];

    public static async Task<MonzoParseResult> ParseAsync(
        Stream csvStream,
        CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new(csvStream);
        CsvConfiguration config = new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using CsvReader csv = new(reader, config);
        if (!await csv.ReadAsync())
        {
            return new MonzoParseResult([], [new MonzoParseError(0, "Empty file")]);
        }

        csv.ReadHeader();
        string[] headers = csv.HeaderRecord ?? [];
        foreach (string required in RequiredHeaders)
        {
            if (!headers.Contains(required, StringComparer.Ordinal))
            {
                return new MonzoParseResult([], [new MonzoParseError(1, $"Missing required header '{required}'")]);
            }
        }

        List<MonzoCsvRow> rows = new();
        List<MonzoParseError> errors = new();
        int rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                string currency = csv.GetField("Currency") ?? string.Empty;
                if (!string.Equals(currency, "GBP", StringComparison.Ordinal))
                {
                    errors.Add(new MonzoParseError(rowNumber, "Non-GBP currency"));
                    continue;
                }

                string dateText = csv.GetField("Date") ?? string.Empty;
                if (!DateOnly.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
                {
                    errors.Add(new MonzoParseError(rowNumber, "Invalid date"));
                    continue;
                }

                TimeOnly? time = null;
                string? timeText = csv.GetField("Time");
                if (!string.IsNullOrWhiteSpace(timeText)
                    && TimeOnly.TryParseExact(timeText, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
                {
                    time = parsedTime;
                }

                decimal amount = decimal.Parse(csv.GetField("Amount") ?? "0", CultureInfo.InvariantCulture);
                decimal? moneyOut = ParseOptionalDecimal(csv.GetField("Money Out"));
                decimal? moneyIn = ParseOptionalDecimal(csv.GetField("Money In"));

                rows.Add(new MonzoCsvRow(
                    csv.GetField("Transaction ID"),
                    date,
                    time,
                    csv.GetField("Type") ?? string.Empty,
                    csv.GetField("Name") ?? string.Empty,
                    amount,
                    currency,
                    csv.GetField("Description"),
                    moneyOut,
                    moneyIn));
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException)
            {
                errors.Add(new MonzoParseError(rowNumber, "Invalid row"));
            }
        }

        return new MonzoParseResult(rows, errors);
    }

    private static decimal? ParseOptionalDecimal(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : decimal.Parse(value, CultureInfo.InvariantCulture);
}
