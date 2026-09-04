using System.Net;
using System.Net.Http.Json;
using BackTemplate.Api.Auth;
using BackTemplate.Tests.TestFixtures;
using Xunit;

namespace BackTemplate.Tests.Integration;

/// <summary>
/// Bate na API pelo pipeline HTTP de verdade (roteamento, model binding,
/// autenticação, rate limiting) em vez de chamar o service direto.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AuthEndpointsTests(PostgresFixture fixture) : IAsyncLifetime
{
    private ApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await _factory.EnsureDatabaseCreatedAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return _factory.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Register_then_login_returns_token()
    {
        var email = $"{Guid.NewGuid()}@example.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "correct-horse-battery-staple"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "correct-horse-battery-staple"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Token));
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_409()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "first-password-123"));

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "second-password-456"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Books_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Books_with_valid_token_returns_200()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "correct-horse-battery-staple"));
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);
        var response = await _client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
