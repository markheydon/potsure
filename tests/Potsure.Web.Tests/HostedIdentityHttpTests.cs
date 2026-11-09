using System.Net;
using System.Text.RegularExpressions;

namespace Potsure.Web.Tests;

public sealed class HostedIdentityHttpTests : IClassFixture<PotsureWebApplicationFactory>
{
    private readonly PotsureWebApplicationFactory _factory;

    public HostedIdentityHttpTests(PotsureWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task HostedMode_RegisterAndLoginPages_RespondOverHttp()
    {
        HttpClient client = _factory.CreateClient();
        using HttpResponseMessage register = await client.GetAsync("/Account/Register", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        string registerHtml = await register.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(ExtractAntiforgeryToken(registerHtml)));

        using HttpResponseMessage login = await client.GetAsync("/Account/Login", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        string loginHtml = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(ExtractAntiforgeryToken(loginHtml)));
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        Match match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        match = Regex.Match(
            html,
            "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
