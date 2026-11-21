namespace Potsure.Infrastructure.Features.Export;

/// <summary>
/// Orchestrates user data export with format choice and reauthentication (FR-M022).
/// </summary>
public sealed class ExportService(
    ExportSnapshotBuilder snapshotBuilder,
    IExportReauthenticationGate reauthenticationGate)
{
    public async Task<ExportResult?> ExportAsync(
        ExportFormat format,
        CancellationToken cancellationToken = default)
    {
        if (!await reauthenticationGate.ConfirmAsync(cancellationToken))
        {
            return null;
        }

        UserFinancialExportSnapshot snapshot = await snapshotBuilder.BuildAsync(cancellationToken);
        return format switch
        {
            ExportFormat.JsonBundle => new ExportResult(
                $"potsure-export-{snapshot.ExportedAt:yyyyMMdd-HHmmss}.json",
                "application/json",
                JsonBundleExporter.Export(snapshot)),
            ExportFormat.CsvZip => new ExportResult(
                $"potsure-export-{snapshot.ExportedAt:yyyyMMdd-HHmmss}.zip",
                "application/zip",
                CsvZipExporter.Export(snapshot)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };
    }
}
