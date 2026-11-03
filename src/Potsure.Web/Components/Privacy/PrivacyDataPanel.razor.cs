using System.Globalization;
using Microsoft.AspNetCore.Components;
using Potsure.Infrastructure.Deletion;
using Potsure.Infrastructure.Export;

namespace Potsure.Web.Components.Privacy;

public partial class PrivacyDataPanel
{
    [Inject]
    private ExportService ExportService { get; set; } = null!;

    [Inject]
    private UserDataDeletionService DeletionService { get; set; } = null!;

    private ExportFormat _exportFormat = ExportFormat.JsonBundle;
    private bool _busy;
    private bool _confirmDelete;
    private string? _exportMessage;
    private string? _deleteMessage;
    private string? _downloadBase64;
    private string? _downloadFileName;
    private string? _downloadContentType;

    private async Task ExportAsync()
    {
        _busy = true;
        _exportMessage = null;
        _downloadBase64 = null;
        try
        {
            ExportResult? result = await ExportService.ExportAsync(_exportFormat);
            if (result is null)
            {
                _exportMessage = "Export cancelled — reauthentication required.";
                return;
            }

            _downloadFileName = result.FileName;
            _downloadContentType = result.ContentType;
            _downloadBase64 = Convert.ToBase64String(result.Content);
            _exportMessage = string.Format(CultureInfo.InvariantCulture, "Export ready ({0}).", result.FileName);
        }
        catch (Exception ex)
        {
            _exportMessage = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DeleteAsync()
    {
        _busy = true;
        _deleteMessage = null;
        try
        {
            bool deleted = await DeletionService.DeleteAllFinancialDataAsync();
            _deleteMessage = deleted
                ? "All financial data has been deleted."
                : "Deletion cancelled — reauthentication required.";
        }
        catch (Exception ex)
        {
            _deleteMessage = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }
}
