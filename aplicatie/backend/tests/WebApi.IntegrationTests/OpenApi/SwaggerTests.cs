using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace NutriScan.WebApi.IntegrationTests.OpenApi;

[Collection(IntegrationTestCollection.Name)]
public class SwaggerTests(NutriScanApiFactory factory)
{
    private async Task<JsonElement> SwaggerAsync()
    {
        var response = await factory.CreateApiClient().GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private static IEnumerable<(string Route, JsonElement Operation)> Operations(JsonElement swagger) =>
        swagger.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(op => ($"{op.Name.ToUpperInvariant()} {path.Name}", op.Value)));

    [Fact]
    public async Task EveryOperation_HasASummaryFromXmlComments()
    {
        // Act
        var undocumented = Operations(await SwaggerAsync())
            .Where(o => !o.Operation.TryGetProperty("summary", out var s) || string.IsNullOrWhiteSpace(s.GetString()))
            .Select(o => o.Route)
            .ToList();

        // Assert
        undocumented.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanEndpoint_IsDocumentedAsFileUpload()
    {
        // Act
        var swagger = await SwaggerAsync();

        // Assert
        var body = swagger.GetProperty("paths").GetProperty("/api/scans").GetProperty("post").GetProperty("requestBody");
        body.GetProperty("content").GetProperty("multipart/form-data").GetProperty("schema").GetProperty("properties")
            .GetProperty("file").GetProperty("format").GetString().Should().Be("binary");
    }
}
