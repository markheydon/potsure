using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Potsure.Infrastructure.Features.Export;

/// <summary>
/// Builds the CSV ZIP export (transactions.csv, plan.csv, metadata.json).
/// </summary>
public static class CsvZipExporter
{
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new() { WriteIndented = true };

    public static byte[] Export(UserFinancialExportSnapshot snapshot)
    {
        using MemoryStream zipStream = new();
        using (ZipArchive archive = new(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteTransactionsCsv(archive, snapshot);
            WritePlanCsv(archive, snapshot);
            WriteMetadataJson(archive, snapshot);
        }

        return zipStream.ToArray();
    }

    private static void WriteTransactionsCsv(ZipArchive archive, UserFinancialExportSnapshot snapshot)
    {
        ZipArchiveEntry entry = archive.CreateEntry("transactions.csv");
        using StreamWriter writer = new(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.WriteLine(
            "Id,MonzoTransactionId,Date,Time,Type,Name,Amount,Currency,Description,Kind,CategoryAssignment,MappedPot,Tag");

        foreach (TransactionExportRow row in snapshot.Transactions)
        {
            writer.WriteLine(string.Join(',', new[]
            {
                Csv(row.Id.ToString()),
                Csv(row.MonzoTransactionId),
                Csv(row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(row.Time?.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
                Csv(row.Type),
                Csv(row.Name),
                Csv(row.Amount.ToString("F2", CultureInfo.InvariantCulture)),
                Csv(row.Currency),
                Csv(row.Description),
                Csv(row.Kind.ToString()),
                Csv(row.CategoryKind?.ToString()),
                Csv(row.MappedPotName),
                Csv(row.Tag),
            }));
        }
    }

    private static void WritePlanCsv(ZipArchive archive, UserFinancialExportSnapshot snapshot)
    {
        ZipArchiveEntry entry = archive.CreateEntry("plan.csv");
        using StreamWriter writer = new(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.WriteLine("Section,Key,Value");
        WritePlanRow(writer, "preset", "SelectedPreset", snapshot.Preset.ToString());
        WritePlanRow(writer, "period", "Start", snapshot.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        WritePlanRow(writer, "period", "End", snapshot.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        WritePlanRow(writer, "period", "BudgetedIncome", snapshot.BudgetedIncome.ToString("F2", CultureInfo.InvariantCulture));

        foreach (CategoryExportRow category in snapshot.Categories)
        {
            WritePlanRow(
                writer,
                "category",
                category.Kind.ToString(),
                category.Allocation.ToString("F2", CultureInfo.InvariantCulture));
        }

        foreach (RecurringExpenseExportRow expense in snapshot.RecurringExpenses)
        {
            WritePlanRow(
                writer,
                "recurringExpense",
                expense.VendorLabel,
                $"{expense.CategoryKind};{expense.Cadence};{expense.AmountKind}");
        }
    }

    private static void WritePlanRow(StreamWriter writer, string section, string key, string value) =>
        writer.WriteLine(string.Join(',', Csv(section), Csv(key), Csv(value)));

    private static void WriteMetadataJson(ZipArchive archive, UserFinancialExportSnapshot snapshot)
    {
        ZipArchiveEntry entry = archive.CreateEntry("metadata.json");
        using StreamWriter writer = new(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        object metadata = new
        {
            exportVersion = "1.0",
            format = "csv-zip",
            exportedAt = snapshot.ExportedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            schemaUrls = new[] { "specs/001-potsure-v1/contracts/export-formats.md" },
        };
        writer.Write(JsonSerializer.Serialize(metadata, MetadataSerializerOptions));
    }

    private static string Csv(string? value)
    {
        string text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n'))
        {
            return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return text;
    }
}
