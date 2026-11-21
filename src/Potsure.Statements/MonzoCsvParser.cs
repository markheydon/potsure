using System.Globalization;
using CsvHelper;

namespace Potsure.Statements;

/// <summary>
/// Parses Monzo CSV exports with auto-detected column maps (FR-M002).
/// </summary>
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

    public static Task<MonzoParseResult> ParseAsync(
        Stream csvStream,
        CancellationToken cancellationToken = default) =>
        ParseAsync(csvStream, columnMap: null, cancellationToken);

    public static async Task<MonzoParseResult> ParseAsync(
        Stream csvStream,
        IReadOnlyDictionary<string, string>? columnMap,
        CancellationToken cancellationToken = default)
    {
        CsvReaderOptions options = CsvReaderOptions.Default with
        {
            HasHeader = true,
            MissingField = CsvMissingFieldHandling.ReturnEmpty,
            TrackLineNumbers = true,
        };

        using CsvReader csv = CsvReader.Create(csvStream, options, leaveOpen: true);
        await csv.ReadHeaderAsync(cancellationToken);
        if (csv.Headers.Count == 0)
        {
            return new MonzoParseResult([], [new MonzoParseError(0, "Empty file")]);
        }

        IReadOnlyList<string> headers = csv.Headers;
        foreach (string required in RequiredHeaders)
        {
            string mapped = MapHeader(columnMap, required);
            if (!headers.Contains(mapped, StringComparer.Ordinal))
            {
                return new MonzoParseResult([], [new MonzoParseError(1, $"Missing required header '{mapped}'")]);
            }
        }

        List<MonzoCsvRow> rows = new();
        List<MonzoParseError> errors = new();
        while (await csv.ReadAsync(cancellationToken))
        {
            int rowNumber = Convert.ToInt32(csv.LineNumber);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                string currency = csv.GetString(MapHeader(columnMap, "Currency"));
                if (!string.Equals(currency, "GBP", StringComparison.Ordinal))
                {
                    errors.Add(new MonzoParseError(rowNumber, "Non-GBP currency"));
                    continue;
                }

                string dateText = csv.GetString(MapHeader(columnMap, "Date"));
                if (!DateOnly.TryParseExact(dateText, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
                {
                    errors.Add(new MonzoParseError(rowNumber, "Invalid date"));
                    continue;
                }

                TimeOnly? time = null;
                if (csv.TryGetFieldIndex(MapHeader(columnMap, "Time"), out int timeIndex) && !csv.IsEmpty(timeIndex))
                {
                    string timeText = csv.GetString(timeIndex);
                    if (TimeOnly.TryParseExact(timeText, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
                    {
                        time = parsedTime;
                    }
                }

                decimal amount = decimal.Parse(csv.GetString(MapHeader(columnMap, "Amount")), CultureInfo.InvariantCulture);
                decimal? moneyOut = TryGetOptionalDecimal(csv, MapHeader(columnMap, "Money Out"));
                decimal? moneyIn = TryGetOptionalDecimal(csv, MapHeader(columnMap, "Money In"));

                string? transactionId = null;
                if (csv.TryGetFieldIndex(MapHeader(columnMap, "Transaction ID"), out int transactionIdIndex) && !csv.IsEmpty(transactionIdIndex))
                {
                    transactionId = csv.GetString(transactionIdIndex);
                }

                string? description = null;
                if (csv.TryGetFieldIndex(MapHeader(columnMap, "Description"), out int descriptionIndex) && !csv.IsEmpty(descriptionIndex))
                {
                    description = csv.GetString(descriptionIndex);
                }

                rows.Add(new MonzoCsvRow(
                    transactionId,
                    date,
                    time,
                    csv.GetString(MapHeader(columnMap, "Type")),
                    csv.GetString(MapHeader(columnMap, "Name")),
                    amount,
                    currency,
                    description,
                    moneyOut,
                    moneyIn));
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException or KeyNotFoundException)
            {
                errors.Add(new MonzoParseError(rowNumber, "Invalid row"));
            }
        }

        return new MonzoParseResult(rows, errors);
    }

    private static decimal? TryGetOptionalDecimal(CsvReader csv, string header)
    {
        if (!csv.TryGetFieldIndex(header, out int index) || csv.IsEmpty(index))
        {
            return null;
        }

        string value = csv.GetString(index);
        return string.IsNullOrWhiteSpace(value)
            ? null
            : decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    public static IReadOnlyList<string> StandardHeaders() => RequiredHeaders;

    private static string MapHeader(IReadOnlyDictionary<string, string>? columnMap, string logical) =>
        columnMap is not null && columnMap.TryGetValue(logical, out string? mapped) && !string.IsNullOrWhiteSpace(mapped)
            ? mapped
            : logical;

    /// <summary>
    /// Reads CSV headers and validates required Monzo columns for mapping confirmation (FR-M002).
    /// </summary>
    public static async Task<MonzoColumnMappingPreview> PreviewColumnMappingAsync(
        Stream csvStream,
        CancellationToken cancellationToken = default)
    {
        CsvReaderOptions options = CsvReaderOptions.Default with
        {
            HasHeader = true,
            MissingField = CsvMissingFieldHandling.ReturnEmpty,
        };

        using CsvReader csv = CsvReader.Create(csvStream, options, leaveOpen: true);
        await csv.ReadHeaderAsync(cancellationToken);
        if (csv.Headers.Count == 0)
        {
            return new MonzoColumnMappingPreview([], RequiredHeaders, RequiredHeaders, false);
        }

        IReadOnlyList<string> headers = csv.Headers;
        List<string> missing = RequiredHeaders
            .Where(required => !headers.Contains(required, StringComparer.Ordinal))
            .ToList();

        return new MonzoColumnMappingPreview(headers, RequiredHeaders, missing, missing.Count == 0);
    }
}
