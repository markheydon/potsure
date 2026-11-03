using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Potsure.Infrastructure.StatementImport;
using Potsure.Statements;
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
    private bool _awaitingMappingConfirmation;
    private bool _accountConfirmed;
    private bool _mappingConfirmed;
    private string? _accountFingerprint;
    private string? _errorMessage;
    private StatementImportSummary? _summary;
    private StatementImportPreview? _preview;
    private Dictionary<string, string> _columnMapping = new(StringComparer.Ordinal);

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs args)
    {
        _errorMessage = null;
        _summary = null;
        _preview = null;
        _awaitingAccountConfirmation = false;
        _awaitingMappingConfirmation = false;
        _accountConfirmed = false;
        _mappingConfirmed = false;

        IBrowserFile file = args.File;
        _selectedFileName = file.Name;
        await using Stream stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer);
        UserInputSanitizer.EnsureUploadSize(buffer.Length);
        _pendingBytes = buffer.ToArray();
        _pendingFile = file;

        await LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
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
            _preview = await ImportService.PreviewAsync(
                stream,
                _pendingFile.Name,
                confirmAccount: false,
                confirmedFingerprint: null);

            if (!_preview.ColumnMapping.LayoutSupported)
            {
                _errorMessage = "This file does not match a supported Monzo export layout.";
                _awaitingMappingConfirmation = false;
                return;
            }

            _awaitingMappingConfirmation = true;
            _columnMapping = BuildDefaultColumnMapping(_preview);

            RememberedMonzoColumnMapping? remembered =
                await ImportService.GetRememberedColumnMappingAsync();
            if (remembered is not null && remembered.MatchesLayout(_preview.ColumnMapping.FileHeaders))
            {
                _columnMapping = remembered.ColumnMap.ToDictionary(
                    static pair => pair.Key,
                    static pair => pair.Value,
                    StringComparer.Ordinal);
            }
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

    private Task ConfirmMappingAsync()
    {
        _mappingConfirmed = true;
        return RunImportAsync(confirmAccount: false, fingerprint: null);
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

        if (_awaitingMappingConfirmation && !_mappingConfirmed && !confirmAccount)
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
                fingerprint,
                _columnMapping);

            if (result.RequiresAccountConfirmation)
            {
                _awaitingAccountConfirmation = true;
                _awaitingMappingConfirmation = false;
                _summary = null;
                return;
            }

            _summary = result;
            _awaitingAccountConfirmation = false;
            _awaitingMappingConfirmation = false;
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
        _preview = null;
        _pendingFile = null;
        _pendingBytes = null;
        _selectedFileName = null;
        _awaitingAccountConfirmation = false;
        _awaitingMappingConfirmation = false;
        _accountConfirmed = false;
        _mappingConfirmed = false;
        _accountFingerprint = null;
        _errorMessage = null;
    }

    private static Dictionary<string, string> BuildDefaultColumnMapping(StatementImportPreview preview)
    {
        Dictionary<string, string> mapping = new(StringComparer.Ordinal);
        foreach (string required in preview.ColumnMapping.RequiredHeaders)
        {
            string match = required;
            foreach (string fileHeader in preview.ColumnMapping.FileHeaders)
            {
                if (string.Equals(fileHeader, required, StringComparison.OrdinalIgnoreCase))
                {
                    match = fileHeader;
                    break;
                }
            }

            mapping[required] = match;
        }

        return mapping;
    }
}
