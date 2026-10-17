using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NutriScan.WebApi.IntegrationTests.Auth;

namespace NutriScan.WebApi.IntegrationTests.Scans;

[Collection(IntegrationTestCollection.Name)]
public class ScansTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    private const int TenMegabytes = 10 * 1024 * 1024;

    // A PNG signature followed by padding: enough for the API, which only checks the signature.
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = factory.CreateApiClient();
        client.UseBearer(await (await client.RegisterAsync()).ReadAccessTokenAsync());
        return client;
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] bytes, string fileName, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        return client.PostAsync("/api/scans", form);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task ValidImage_WhileOcrIsStubbed_Returns200Unavailable()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await UploadAsync(client, Png, "eticheta.png", "image/png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonAsync(response);
        json.GetProperty("status").GetString().Should().Be("unavailable");
        json.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("fields").EnumerateObject().Should().HaveCount(8)
            .And.AllSatisfy(f => f.Value.ValueKind.Should().Be(JsonValueKind.Null));
    }

    [Fact]
    public async Task TextRenamedAsJpeg_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await UploadAsync(client, Encoding.UTF8.GetBytes("<script>alert(1)</script>"), "eticheta.jpg", "image/jpeg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await JsonAsync(response)).GetProperty("errors").GetProperty("Image")[0].GetString()
            .Should().Be("Fotografia trebuie să fie JPEG, PNG sau WebP.");
    }

    [Fact]
    public async Task NoFile_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.PostAsync("/api/scans", new MultipartFormDataContent { { new StringContent("x"), "other" } });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(TenMegabytes + 1)]
    [InlineData(TenMegabytes + 2 * 1024 * 1024)]
    public async Task OverTenMegabytes_Returns413(int size)
    {
        // Arrange
        var client = await SignedInClientAsync();
        var image = new byte[size];
        Png.CopyTo(image, 0);

        // Act
        var response = await UploadAsync(client, image, "mare.png", "image/png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WithoutToken_Returns401()
    {
        // Act
        var response = await UploadAsync(factory.CreateApiClient(), Png, "eticheta.png", "image/png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
