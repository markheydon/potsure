using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Potsure.Infrastructure.StatementImport;
using Potsure.Web.Validation;

namespace Potsure.Web.Components.Import;

public partial class ImportWizard
{
    [Inject]
    private StatementImportService ImportService { get; set; } = null!;

    private IBrowserFile? _pendingFile;
    private string? _selectedFileName;
    private byte[]? _pendingBytes;
    private bool _isBusy;
    private bool _awaitingAccountConfirmation;
    private bool _accountConfirmed;
    private string? _accountFingerprint;
    private string? _errorMessage;
    private StatementImportSummary? _summary;

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs args)
    {
        _errorMessage = null;
        _summary = null;
        _awaitingAccountConfirmation = false;
        _accountConfirmed = false;

        IBrowserFile file = args.File;
        _selectedFileName = file.Name;
        await using Stream stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer);
        UserInputSanitizer.EnsureUploadSize(buffer.Length);
        _pendingBytes = buffer.ToArray();
        _pendingFile = file;
    }

    private Task ImportWithoutConfirmationAsync() =>
        RunImportAsync(confirmAccount: false, fingerprint: null);

    private Task ConfirmAccountAndImportAsync() =>
        RunImportAsync(confirmAccount: true, fingerprint: _accountFingerprint);

    private async Task RunImportAsync(bool confirmAccount, string? fingerprint)
    {
        if (_pendingBytes is null || _pendingFile is null)
        {
            return;
        }

        _isBusy = true;
        _errorMessage = null;
        try
        {
            await using MemoryStream stream = new(_pendingBytes);
            StatementImportSummary result = await ImportService.ImportAsync(
                stream,
                _pendingFile.Name,
                confirmAccount,
                fingerprint);

            if (result.RequiresAccountConfirmation)
            {
                _awaitingAccountConfirmation = true;
                _summary = null;
                return;
            }

            _summary = result;
            _awaitingAccountConfirmation = false;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void ResetWizard()
    {
        _summary = null;
        _pendingFile = null;
        _pendingBytes = null;
        _selectedFileName = null;
        _awaitingAccountConfirmation = false;
        _accountConfirmed = false;
        _accountFingerprint = null;
        _errorMessage = null;
    }
}
