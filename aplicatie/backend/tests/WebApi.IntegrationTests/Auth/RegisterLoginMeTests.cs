using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace NutriScan.WebApi.IntegrationTests.Auth;

[Collection(IntegrationTestCollection.Name)]
public class RegisterLoginMeTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ValidRequest_Returns201WithTokenAndSecureCookie()
    {
        // Arrange
        var client = factory.CreateApiClient();

        // Act
        var response = await client.RegisterAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        json.RootElement.GetProperty("user").GetProperty("isProfileComplete").GetBoolean().Should().BeFalse();
        body.Should().NotContain("refreshToken");

        var cookie = response.RefreshCookieHeader()!.ToLowerInvariant();
        cookie.Should().Contain("httponly").And.Contain("secure").And.Contain("samesite=strict").And.Contain("path=/api/auth");
        response.RefreshCookieValue().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmailDifferentCase_Returns409()
    {
        // Arrange
        var client = factory.CreateApiClient();
        await client.RegisterAsync("ana@exemplu.ro");

        // Act
        var response = await client.RegisterAsync("ANA@exemplu.ro");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task Register_WithEmptyBody_Returns400(string body)
    {
        // Arrange
        var client = factory.CreateApiClient();

        // Act
        var response = await client.PostAsync("/api/auth/register", new StringContent(body, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithCookie()
    {
        // Arrange
        var client = factory.CreateApiClient();
        await client.RegisterAsync();

        // Act
        var response = await client.LoginAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.RefreshCookieValue().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401Generic()
    {
        // Arrange
        var client = factory.CreateApiClient();
        await client.RegisterAsync();

        // Act
        var response = await client.LoginAsync(password: "parola-gresita");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("title").GetString().Should().Be("Email sau parolă incorectă");
    }

    [Fact]
    public async Task Login_AfterFiveFailures_CorrectPasswordStillReturns401()
    {
        // Arrange
        var client = factory.CreateApiClient();
        await client.RegisterAsync();
        for (var i = 0; i < 5; i++)
        {
            await client.LoginAsync(password: "parola-gresita");
        }

        // Act
        var response = await client.LoginAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        // Act
        var response = await factory.CreateApiClient().GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithGarbageToken_Returns401()
    {
        // Arrange
        var client = factory.CreateApiClient();
        client.UseBearer("not-a-jwt");

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithAccessToken_Returns200()
    {
        // Arrange
        var client = factory.CreateApiClient();
        var register = await client.RegisterAsync("  Ana@Exemplu.RO ");
        client.UseBearer(await register.ReadAccessTokenAsync());

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<JsonElement>();
        user.GetProperty("email").GetString().Should().Be("ana@exemplu.ro");
    }
}
