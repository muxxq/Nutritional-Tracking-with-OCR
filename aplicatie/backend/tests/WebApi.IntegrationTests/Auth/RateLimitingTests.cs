using System.Net;
using FluentAssertions;

namespace NutriScan.WebApi.IntegrationTests.Auth;

public class RateLimitedApiFactory : NutriScanApiFactory
{
    protected override int AuthPermitLimit => 10;
}

public class RateLimitingTests(RateLimitedApiFactory factory) : IClassFixture<RateLimitedApiFactory>
{
    [Fact]
    public async Task AuthEndpoints_EleventhRequestInWindow_Returns429()
    {
        // Arrange
        var client = factory.CreateApiClient();
        var statuses = new List<HttpStatusCode>();

        // Act
        for (var i = 0; i < 11; i++)
        {
            statuses.Add((await client.LoginAsync("nimeni@exemplu.ro")).StatusCode);
        }

        // Assert
        statuses.Take(10).Should().NotContain(HttpStatusCode.TooManyRequests);
        statuses[10].Should().Be(HttpStatusCode.TooManyRequests);
    }
}
