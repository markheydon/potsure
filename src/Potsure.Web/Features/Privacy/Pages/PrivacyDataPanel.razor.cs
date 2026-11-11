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
    private string _hostedPassword = string.Empty;
    private string? _exportMessage;
    private string? _exportReauthError;
    private string? _deleteMessage;
    private string? _deleteReauthError;
    private string? _downloadBase64;
    private string? _downloadFileName;
    private string? _downloadContentType;

    private const string ExportPhrase = "EXPORT";
    private const string DeletePhrase = "DELETE";

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
        _hostedPassword = string.Empty;
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
        _hostedPassword = string.Empty;
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
            string? hostedError = await ValidateHostedPasswordAsync();
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
            string? hostedError = await ValidateHostedPasswordAsync();
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

    private Task SetHostedPasswordAsync(string? value)
    {
        _hostedPassword = value ?? string.Empty;
        return Task.CompletedTask;
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

    private async Task<string?> ValidateHostedPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(_hostedPassword))
        {
            return "Enter your password to continue.";
        }

        if (!await PasswordReauthenticationService.ValidatePasswordAsync(_hostedPassword))
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
                _exportMessage = "Export cancelled  -  reauthentication required.";
                return;
            }

            _downloadFileName = result.FileName;
            _downloadContentType = result.ContentType;
            _downloadBase64 = Convert.ToBase64String(result.Content);
            _exportMessage = string.Format(CultureInfo.InvariantCulture, "Export ready ({0}).", result.FileName);
        }
        catch (Exception)
        {
            _exportMessage = "Export failed. Try again or contact your administrator.";
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
                : "Deletion cancelled  -  reauthentication required.";
        }
        catch (Exception)
        {
            _deleteMessage = "Deletion failed. Try again or contact your administrator.";
        }
        finally
        {
            _busy = false;
        }
    }
}
