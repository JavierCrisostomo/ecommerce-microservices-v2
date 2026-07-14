using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Identity.Api.Contracts;
using Identity.Application.Users.Commands.Login;
using Identity.Application.Users.Commands.RegisterUser;
using IntegrationTests.Shared;
using Xunit;

namespace Identity.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private readonly IdentityApiFactory _factory;
    private HttpClient _client = null!;

    public AuthEndpointsTests(SqlServerFixture sqlFixture)
    {
        _factory = new IdentityApiFactory(sqlFixture.GetConnectionString("identity_db_it"));
    }

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Register_WithNewEmail_ReturnsCreatedWithUser()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("nueva@example.com", "SuperSecret123"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RegisterUserResult>();
        body!.Email.Should().Be("nueva@example.com");
        body.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_WithEmailAlreadyTaken_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("repetida@example.com", "SuperSecret123"));

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("repetida@example.com", "OtherSecret123"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("debil@example.com", "corta"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("login@example.com", "SuperSecret123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("login@example.com", "SuperSecret123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("wrongpass@example.com", "SuperSecret123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("wrongpass@example.com", "NotTheRightOne"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserClaims()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("me@example.com", "SuperSecret123"));
        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("me@example.com", "SuperSecret123"));
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("email").GetString().Should().Be("me@example.com");
    }
}
