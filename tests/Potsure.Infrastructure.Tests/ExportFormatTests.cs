using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Budgeting;
using Potsure.Infrastructure.Features.Export;
using Potsure.Infrastructure.Features.Import;

namespace Potsure.Infrastructure.Tests;

public sealed class ExportFormatTests
{
    [Fact]
    public async Task ExportService_AfterGoldenMonzoImport_ProducesJsonBundle()
    {
        const string owner = "export-after-import";
        string fixturePath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Potsure.Statements.Tests",
                "Fixtures",
                "monzo-golden-valid.csv"));
        Assert.True(File.Exists(fixturePath), $"Fixture missing at {fixturePath}");

        ServiceProvider provider = DocumentStoreTestHarness.BuildProvider(
            owner,
            reauthenticationGate: new AllowExportReauthenticationGate());

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        StatementImportService importService = scope.ServiceProvider.GetRequiredService<StatementImportService>();
        ExportService exportService = scope.ServiceProvider.GetRequiredService<ExportService>();

        await using FileStream fixture = File.OpenRead(fixturePath);
        StatementImportSummary importSummary = await importService.ImportAsync(
            fixture,
            "monzo-golden-valid.csv",
            userConfirmedAccount: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(importSummary.RequiresAccountConfirmation);
        Assert.True(importSummary.ImportedCount > 0, "Expected transactions to import.");

        ExportResult? export = await exportService.ExportAsync(
            ExportFormat.JsonBundle,
            TestContext.Current.CancellationToken);

        Assert.NotNull(export);
        Assert.True(export!.Content.Length > 0);
    }
    [Fact]
    public void JsonBundle_ContainsExportVersionAndCurrency()
    {
        UserFinancialExportSnapshot snapshot = SampleSnapshot();
        byte[] json = JsonBundleExporter.Export(snapshot);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal("1.0", document.RootElement.GetProperty("exportVersion").GetString());
        Assert.Equal("GBP", document.RootElement.GetProperty("currency").GetString());
        Assert.Equal(
            snapshot.EstimatedIncomeAmount,
            document.RootElement
                .GetProperty("budgetProfile")
                .GetProperty("standingPlan")
                .GetProperty("estimatedIncomeAmount")
                .GetDecimal());
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
            3,
            2500m,
            PlanIncomeCadence.Monthly,
            PotMappingMode.CategoryOnly,
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

    private sealed class AllowExportReauthenticationGate : IExportReauthenticationGate
    {
        public Task<bool> ConfirmAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
