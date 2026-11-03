namespace Potsure.Web.Validation;

/// <summary>
/// Shared input validation for uploads and forms (FR-M028).
/// </summary>
public static class UserInputSanitizer
{
    private const int MaxLabelLength = 200;
    private const long MaxUploadBytes = 20 * 1024 * 1024;

    /// <summary>
    /// Trims and bounds free-text labels entered in forms.
    /// </summary>
    public static string SanitiseLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= MaxLabelLength ? trimmed : trimmed[..MaxLabelLength];
    }

    /// <summary>
    /// Validates upload size before streaming parse.
    /// </summary>
    public static void EnsureUploadSize(long length)
    {
        if (length <= 0)
        {
            throw new InvalidOperationException("Upload is empty.");
        }

        if (length > MaxUploadBytes)
        {
            throw new InvalidOperationException("Upload exceeds the maximum allowed size.");
        }
    }
}
