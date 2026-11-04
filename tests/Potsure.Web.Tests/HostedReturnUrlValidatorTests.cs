namespace Potsure.Web.Tests;

public sealed class HostedReturnUrlValidatorTests
{
    [Theory]
    [InlineData("/plan", "/plan")]
    [InlineData("/import?step=2", "/import?step=2")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("https://evil.example/", "/")]
    [InlineData("//evil.example/", "/")]
    [InlineData("/\\evil", "/")]
    [InlineData("import", "/import")]
    [InlineData("Account/Lockout", "/Account/Lockout")]
    public void Normalize_AcceptsLocalPathsOnly(string? input, string expected)
    {
        string actual = HostedReturnUrlValidator.Normalize(input);
        Assert.Equal(expected, actual);
    }
}
