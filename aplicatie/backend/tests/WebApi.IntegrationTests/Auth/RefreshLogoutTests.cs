using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NutriScan.WebApi.IntegrationTests.Auth;

[Collection(IntegrationTestCollection.Name)]
public class RefreshLogoutTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateClientWithoutCookies() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });

    private Task<HttpResponseMessage> RefreshWithCookieAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{AuthApi.CookieName}={refreshToken}");
        return CreateClientWithoutCookies().SendAsync(request);
    }

    private static void ShouldClearCookie(HttpResponseMessage response)
    {
        var header = response.RefreshCookieHeader();
        header.Should().NotBeNull();
        header!.ToLowerInvariant().Should().Contain("expires=thu, 01 jan 1970").And.Contain("path=/api/auth");
    }

    [Fact]
    public async Task Refresh_WithValidCookie_Returns200AndRotatesCookie()
    {
        // Arrange
        var client = factory.CreateApiClient();
        var register = await client.RegisterAsync();
        var original = register.RefreshCookieValue();

        // Act
        var response = await client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadAccessTokenAsync()).Should().NotBeNullOrEmpty();
        response.RefreshCookieValue().Should().NotBeNullOrEmpty().And.NotBe(original);
    }

    [Fact]
    public async Task Refresh_WithReusedToken_Returns401AndClearsCookie()
    {
        // Arrange
        var client = factory.CreateApiClient();
        var rt1 = (await client.RegisterAsync()).RefreshCookieValue()!;
        var rt2 = (await client.PostAsync("/api/auth/refresh", null)).RefreshCookieValue()!;

        // Act
        var reuse = await RefreshWithCookieAsync(rt1);

        // Assert
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ShouldClearCookie(reuse);
        (await RefreshWithCookieAsync(rt2)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        // Act
        var response = await CreateClientWithoutCookies().PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesSessionButNotOtherDevice()
    {
        // Arrange
        var phone = factory.CreateApiClient();
        var laptop = factory.CreateApiClient();
        await phone.RegisterAsync();
        await laptop.LoginAsync();

        // Act
        var logout = await phone.PostAsync("/api/auth/logout", null);

        // Assert
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ShouldClearCookie(logout);
        (await phone.PostAsync("/api/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await laptop.PostAsync("/api/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithUnknownCookie_Returns204AndClearsCookie()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("Cookie", $"{AuthApi.CookieName}=nu-exista");

        // Act
        var response = await CreateClientWithoutCookies().SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        ShouldClearCookie(response);
    }
}
