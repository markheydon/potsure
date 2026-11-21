using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Potsure.Infrastructure.Deletion;
using Potsure.Infrastructure.Features.Export;
using Potsure.Web.Configuration;
using Potsure.Web.Services;

namespace Potsure.Web.Features.Privacy.Pages;

public partial class PrivacyDataPanel
{
    [Inject]
    private ExportService ExportService { get; set; } = null!;

    [Inject]
    private UserDataDeletionService DeletionService { get; set; } = null!;

    [Inject]
    private ReauthenticationService ReauthenticationService { get; set; } = null!;

    [Inject]
    private PasswordReauthenticationService PasswordReauthenticationService { get; set; } = null!;

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
    private string _exportReauthPassword = string.Empty;
    private string _deleteReauthPassword = string.Empty;
    private string? _exportMessage;
    private string? _exportReauthError;
    private string? _deleteMessage;
    private string? _deleteReauthError;
    private string? _downloadBase64;
    private string? _downloadFileName;
    private string? _downloadContentType;

    private const string ExportPhrase = "EXPORT";
    private const string DeletePhrase = "DELETE";

    private int _exportPasswordFieldKey;
    private int _deletePasswordFieldKey;

    protected override void OnInitialized()
    {
        ResetReauthUiState();
    }

    private async Task BeginExportAsync()
    {
        _exportMessage = null;
        _exportReauthError = null;
        _downloadBase64 = null;
        if (ReauthenticationService.IsReauthenticationValid)
        {
            await ExportAsync();
            return;
        }

        _exportChallengePhrase = string.Empty;
        _exportReauthError = null;
        _exportReauthPassword = string.Empty;
        _exportPasswordFieldKey++;
        _showExportReauth = true;
    }

    private async Task BeginDeleteAsync()
    {
        _deleteMessage = null;
        _deleteReauthError = null;
        if (ReauthenticationService.IsReauthenticationValid)
        {
            await DeleteAsync();
            return;
        }

        _deleteChallengePhrase = string.Empty;
        _deleteReauthPassword = string.Empty;
        _deletePasswordFieldKey++;
        _showDeleteReauth = true;
    }

    private static string FormatExportFormat(ExportFormat format) =>
        format switch
        {
            ExportFormat.JsonBundle => "JSON bundle",
            ExportFormat.CsvZip => "CSV ZIP",
            _ => format.ToString(),
        };

    private async Task ConfirmExportReauthAsync()
    {
        _exportReauthError = null;
        if (IsHosted)
        {
            string? hostedError = await ValidateHostedPasswordAsync(_exportReauthPassword);
            if (hostedError is not null)
            {
                _exportReauthError = hostedError;
                return;
            }
        }
        else if (!string.Equals(_exportChallengePhrase.Trim(), ExportPhrase, StringComparison.Ordinal))
        {
            _exportReauthError = $"Type {ExportPhrase} exactly to confirm export.";
            return;
        }

        ReauthenticationService.GrantReauthentication();
        _showExportReauth = false;
        await ExportAsync();
    }

    private async Task ConfirmDeleteReauthAsync()
    {
        _deleteReauthError = null;
        if (IsHosted)
        {
            string? hostedError = await ValidateHostedPasswordAsync(_deleteReauthPassword);
            if (hostedError is not null)
            {
                _deleteReauthError = hostedError;
                return;
            }
        }
        else if (!string.Equals(_deleteChallengePhrase.Trim(), DeletePhrase, StringComparison.Ordinal))
        {
            _deleteReauthError = $"Type {DeletePhrase} exactly to confirm deletion.";
            return;
        }

        ReauthenticationService.GrantReauthentication();
        _showDeleteReauth = false;
        await DeleteAsync();
    }

    private Task SetExportChallengePhraseAsync(string? value)
    {
        _exportChallengePhrase = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task SetDeleteChallengePhraseAsync(string? value)
    {
        _deleteChallengePhrase = value ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task<string?> ValidateHostedPasswordAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Enter your password to continue.";
        }

        if (!await PasswordReauthenticationService.ValidatePasswordAsync(password))
        {
            return "That password is incorrect. Try again.";
        }

        return null;
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
                _showExportReauth = false;
                _exportMessage = "Export cancelled; reauthentication required.";
                return;
            }

            _downloadFileName = result.FileName;
            _downloadContentType = result.ContentType;
            _downloadBase64 = Convert.ToBase64String(result.Content);
            _exportMessage = string.Format(CultureInfo.InvariantCulture, "Export ready ({0}).", result.FileName);
            ReauthenticationService.RevokeReauthentication();
            ClearSensitiveReauthFields();
        }
        catch (Exception)
        {
            _exportMessage = "Export failed. Try again or contact your administrator.";
            ReauthenticationService.RevokeReauthentication();
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
                _deleteMessage = "Deletion cancelled; reauthentication required.";
                return;
            }

            ReauthenticationService.RevokeReauthentication();
            ClearSensitiveReauthFields();
            _showDeleteReauth = false;
            _confirmDelete = false;
            _deleteMessage = "All financial data has been deleted.";
        }
        catch (Exception)
        {
            ReauthenticationService.RevokeReauthentication();
            _deleteMessage = "Deletion failed. Try again or contact your administrator.";
        }
        finally
        {
            _busy = false;
        }
    }

    private void ClearSensitiveReauthFields()
    {
        _exportReauthPassword = string.Empty;
        _deleteReauthPassword = string.Empty;
        _exportChallengePhrase = string.Empty;
        _deleteChallengePhrase = string.Empty;
        _exportPasswordFieldKey++;
        _deletePasswordFieldKey++;
    }

    private void ResetReauthUiState()
    {
        _showExportReauth = false;
        _showDeleteReauth = false;
        _exportReauthError = null;
        _deleteReauthError = null;
        ClearSensitiveReauthFields();
    }

}
