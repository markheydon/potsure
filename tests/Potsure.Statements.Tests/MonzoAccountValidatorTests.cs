using Potsure.Statements;

namespace Potsure.Statements.Tests;

public sealed class MonzoAccountValidatorTests
{
    [Fact]
    public void IsMonzoLayout_ReturnsFalseWhenRequiredHeaderMissing()
    {
        bool result = MonzoAccountValidator.IsMonzoLayout(["Date", "Type", "Name", "Amount"]);
        Assert.False(result);
    }

    [Fact]
    public void IsMonzoLayout_ReturnsTrueForStandardExportHeaders()
    {
        bool result = MonzoAccountValidator.IsMonzoLayout(
        [
            "Date",
            "Type",
            "Name",
            "Amount",
            "Currency",
            "Notes",
        ]);

        Assert.True(result);
    }

    [Theory]
    [InlineData("fp-a", "fp-a", false, MonzoAccountValidationOutcome.Accepted)]
    [InlineData("fp-a", "fp-b", false, MonzoAccountValidationOutcome.DifferentAccountRejected)]
    [InlineData(null, "fp-b", false, MonzoAccountValidationOutcome.ConfirmationRequired)]
    [InlineData(null, "fp-b", true, MonzoAccountValidationOutcome.Accepted)]
    [InlineData("fp-a", null, false, MonzoAccountValidationOutcome.ConfirmationRequired)]
    [InlineData("fp-a", null, true, MonzoAccountValidationOutcome.Accepted)]
    public void ValidateAssociation_MatchesExpectedOutcome(
        string? fileFingerprint,
        string? storedFingerprint,
        bool userConfirmed,
        MonzoAccountValidationOutcome expected)
    {
        MonzoAccountValidationResult result = MonzoAccountValidator.ValidateAssociation(
            fileFingerprint,
            storedFingerprint,
            userConfirmed);

        Assert.Equal(expected, result.Outcome);
    }
}
