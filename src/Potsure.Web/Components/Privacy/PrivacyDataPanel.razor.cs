using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Potsure.Infrastructure.Deletion;
using Potsure.Infrastructure.Export;
using Potsure.Web.Configuration;
using Potsure.Web.Services;

namespace Potsure.Web.Components.Privacy;

public partial class PrivacyDataPanel
{
    [Inject]
    private ExportService ExportService { get; set; } = null!;

    [Inject]
    private UserDataDeletionService DeletionService { get; set; } = null!;

    [Inject]
    private ReauthenticationService ReauthenticationService { get; set; } = null!;

    [Inject]
    private IOptions<DeploymentModeOptions> DeploymentOptions { get; set; } = null!;

    private bool IsHosted => DeploymentOptions.Value.Mode == DeploymentMode.Hosted;
    private ExportFormat _exportFormat = ExportFormat.JsonBundle;
    private bool _busy;
    private bool _confirmDelete;
    private bool _showExportReauth;
    private bool _showDeleteReauth;
    private string _exportChallengePhrase = string.Empty;
    private string _deleteChallengePhrase = string.Empty;
    private bool _hostedPasskeyStepUpConfirmed;
    private string? _exportMessage;
    private string? _deleteMessage;
    private string? _downloadBase64;
    private string? _downloadFileName;
    private string? _downloadContentType;

    private const string ExportPhrase = "EXPORT";
    private const string DeletePhrase = "DELETE";

    private void BeginExport()
    {
        _exportMessage = null;
        _downloadBase64 = null;
        if (ReauthenticationService.IsReauthenticationValid)
        {
            _ = ExportAsync();
            return;
        }

        _exportChallengePhrase = string.Empty;
        _hostedPasskeyStepUpConfirmed = false;
        _showExportReauth = true;
    }

    private void BeginDelete()
    {
        _deleteMessage = null;
        if (ReauthenticationService.IsReauthenticationValid)
        {
            _ = DeleteAsync();
            return;
        }

        _deleteChallengePhrase = string.Empty;
        _hostedPasskeyStepUpConfirmed = false;
        _showDeleteReauth = true;
    }

    private Task ConfirmExportReauthAsync()
    {
        if (IsHosted)
        {
            if (!_hostedPasskeyStepUpConfirmed)
            {
                _exportMessage = "Complete passkey step-up before export.";
                return Task.CompletedTask;
            }
        }
        else if (!string.Equals(_exportChallengePhrase.Trim(), ExportPhrase, StringComparison.Ordinal))
        {
            _exportMessage = $"Type {ExportPhrase} to confirm export.";
            return Task.CompletedTask;
        }

        ReauthenticationService.GrantReauthentication();
        _showExportReauth = false;
        return ExportAsync();
    }

    private Task ConfirmDeleteReauthAsync()
    {
        if (IsHosted)
        {
            if (!_hostedPasskeyStepUpConfirmed)
            {
                _deleteMessage = "Complete passkey step-up before deletion.";
                return Task.CompletedTask;
            }
        }
        else if (!string.Equals(_deleteChallengePhrase.Trim(), DeletePhrase, StringComparison.Ordinal))
        {
            _deleteMessage = $"Type {DeletePhrase} to confirm deletion.";
            return Task.CompletedTask;
        }

        ReauthenticationService.GrantReauthentication();
        _showDeleteReauth = false;
        return DeleteAsync();
    }

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
                ReauthenticationService.RevokeReauthentication();
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
            if (!deleted)
            {
                ReauthenticationService.RevokeReauthentication();
            }

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
