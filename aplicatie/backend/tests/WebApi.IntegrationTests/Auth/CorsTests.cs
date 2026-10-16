using FluentAssertions;

namespace NutriScan.WebApi.IntegrationTests.Auth;

[Collection(IntegrationTestCollection.Name)]
public class CorsTests(NutriScanApiFactory factory)
{
    private Task<HttpResponseMessage> PreflightAsync(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return factory.CreateApiClient().SendAsync(request);
    }

    [Fact]
    public async Task Preflight_FromFrontendOrigin_AllowsCredentials()
    {
        // Act
        var response = await PreflightAsync(NutriScanApiFactory.FrontendOrigin);

        // Assert
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(NutriScanApiFactory.FrontendOrigin);
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().ContainSingle("true");
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_IsNotAllowed()
    {
        // Act
        var response = await PreflightAsync("https://evil.example");

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
