using System.Net;
using System.Text.Json.Nodes;
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

    [Fact]
    public async Task Schemas_FollowNullableReferenceTypes()
    {
        // Act
        var schemas = (await SwaggerAsync()).GetProperty("components").GetProperty("schemas");

        // Assert: the client's generated types rely on these to know what can be null
        var item = schemas.GetProperty("ShoppingListItemDto");
        Required(item).Should().Contain(["id", "name", "category", "isPurchased"]);
        Nullable(item.GetProperty("properties").GetProperty("name")).Should().BeFalse();
        Nullable(item.GetProperty("properties").GetProperty("quantityText")).Should().BeTrue();

        var stats = schemas.GetProperty("StatisticsDto");
        Nullable(stats.GetProperty("properties").GetProperty("average")).Should().BeTrue();
        Nullable(stats.GetProperty("properties").GetProperty("targets")).Should().BeFalse();
    }

    private static IEnumerable<string> Required(JsonElement schema) =>
        schema.TryGetProperty("required", out var r) ? r.EnumerateArray().Select(e => e.GetString()!) : [];

    private static bool Nullable(JsonElement property) =>
        property.TryGetProperty("nullable", out var n) && n.GetBoolean();

    /// <summary>
    /// The committed contract (aplicatie/backend/openapi.json) is what the client's TypeScript types are generated from.
    /// Changing the API without regenerating it fails here; run with UPDATE_OPENAPI=1 to rewrite the file.
    /// </summary>
    [Fact]
    public async Task CommittedOpenApiContract_MatchesTheApi()
    {
        // Arrange
        var path = Path.Combine(BackendDirectory(), "openapi.json");
        var actual = JsonNode.Parse((await SwaggerAsync()).GetRawText())!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI") == "1")
        {
            await File.WriteAllTextAsync(path, actual + "\n");
        }

        // Act
        var committed = File.Exists(path) ? (await File.ReadAllTextAsync(path)).TrimEnd() : "";

        // Assert
        committed.Should().Be(actual, "openapi.json must be regenerated after an API change (UPDATE_OPENAPI=1 dotnet test)");
    }

    private static string BackendDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NutriScan.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("NutriScan.sln not found above the test binaries.");
    }
}
