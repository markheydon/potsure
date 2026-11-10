using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Data;

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

    [Fact]
    public async Task HostedMode_UnauthenticatedFinancialRoute_RedirectsToLogin()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        using HttpResponseMessage response = await client.GetAsync(
            "/import",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HostedMode_PostRegisterAndPasswordSignIn_SetsIdentityCookie()
    {
        await EnsureDatabaseCreatedAsync();

        string email = $"hosted-http-{Guid.NewGuid():N}@example.com";
        const string password = "Password1";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

        string registerToken = await GetAntiforgeryTokenAsync(client, "/Account/Register");
        using HttpResponseMessage registerPost = await client.PostAsync(
            "/Account/Register",
            BuildRegisterForm(email, password, registerToken),
            cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, registerPost.StatusCode);
        Assert.Contains(
            IdentityConstants.ApplicationScheme,
            registerPost.Headers.GetValues("Set-Cookie").First(),
            StringComparison.OrdinalIgnoreCase);

        HttpClient loginClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

        string loginToken = await GetAntiforgeryTokenAsync(loginClient, "/Account/Login");
        using HttpResponseMessage loginPost = await loginClient.PostAsync(
            "/Account/Login",
            BuildLoginForm(email, password, loginToken),
            cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);
        Assert.Contains(
            IdentityConstants.ApplicationScheme,
            loginPost.Headers.GetValues("Set-Cookie").First(),
            StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage import = await loginClient.GetAsync("/import", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, import.StatusCode);
    }

    private async Task EnsureDatabaseCreatedAsync()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        PotsureDbContext db = scope.ServiceProvider.GetRequiredService<PotsureDbContext>();
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return ExtractAntiforgeryToken(html);
    }

    private static FormUrlEncodedContent BuildRegisterForm(string email, string password, string antiforgeryToken)
    {
        Dictionary<string, string> fields = new()
        {
            ["_handler"] = "register",
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password,
            ["__RequestVerificationToken"] = antiforgeryToken,
        };

        return new FormUrlEncodedContent(fields);
    }

    private static FormUrlEncodedContent BuildLoginForm(string email, string password, string antiforgeryToken)
    {
        Dictionary<string, string> fields = new()
        {
            ["_handler"] = "login",
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false",
            ["__RequestVerificationToken"] = antiforgeryToken,
        };

        return new FormUrlEncodedContent(fields);
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
