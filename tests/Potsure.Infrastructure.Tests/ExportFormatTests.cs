using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Potsure.Budgeting;
using Potsure.Infrastructure.Export;

namespace Potsure.Infrastructure.Tests;

public sealed class ExportFormatTests
{
    [Fact]
    public void JsonBundle_ContainsExportVersionAndCurrency()
    {
        UserFinancialExportSnapshot snapshot = SampleSnapshot();
        byte[] json = JsonBundleExporter.Export(snapshot);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal("1.0", document.RootElement.GetProperty("exportVersion").GetString());
        Assert.Equal("GBP", document.RootElement.GetProperty("currency").GetString());
        Assert.Equal(
            snapshot.BudgetedIncome,
            document.RootElement.GetProperty("budgetProfile").GetProperty("period").GetProperty("budgetedIncome").GetDecimal());
    }

    [Fact]
    public void CsvZip_ContainsRequiredEntriesAndMetadata()
    {
        UserFinancialExportSnapshot snapshot = SampleSnapshot();
        byte[] zipBytes = CsvZipExporter.Export(snapshot);
        using MemoryStream stream = new(zipBytes);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("transactions.csv"));
        Assert.NotNull(archive.GetEntry("plan.csv"));
        ZipArchiveEntry? metadata = archive.GetEntry("metadata.json");
        Assert.NotNull(metadata);
        using StreamReader reader = new(metadata!.Open());
        string metadataText = reader.ReadToEnd();
        using JsonDocument document = JsonDocument.Parse(metadataText);
        Assert.Equal("csv-zip", document.RootElement.GetProperty("format").GetString());
        Assert.Equal("1.0", document.RootElement.GetProperty("exportVersion").GetString());
    }

    private static UserFinancialExportSnapshot SampleSnapshot() =>
        new(
            DateTimeOffset.Parse("2026-11-02T12:00:00Z", CultureInfo.InvariantCulture),
            AllocationPreset.FiftyThirtyTwenty,
            3,
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31),
            2500m,
            [
                new CategoryExportRow(BudgetCategoryKind.Needs, 1250m, 0m, 0m),
                new CategoryExportRow(BudgetCategoryKind.Wants, 750m, 0m, 0m),
                new CategoryExportRow(BudgetCategoryKind.SavingsAndDebt, 500m, 0m, 0m),
            ],
            [],
            new MonzoAccountExportRow("Current account", "fp-test"),
            [],
            [],
            [],
            [
                new TransactionExportRow(
                    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    "tx_1",
                    new DateOnly(2026, 10, 5),
                    new TimeOnly(9, 30),
                    "Faster payment",
                    "Employer Ltd",
                    2500m,
                    "GBP",
                    "Salary",
                    TransactionKind.IncomeCredit,
                    null,
                    null,
                    null),
            ]);
}
