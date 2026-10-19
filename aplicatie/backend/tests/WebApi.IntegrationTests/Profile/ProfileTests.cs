using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NutriScan.WebApi.IntegrationTests.Auth;

namespace NutriScan.WebApi.IntegrationTests.Profile;

[Collection(IntegrationTestCollection.Name)]
public class ProfileTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    // Reference case R1 (spec §3.6). The API uses the real clock, so the birth date is relative:
    // 28 years and one day ago keeps the age at 28 whenever the suite runs.
    private static readonly string R1BirthDate =
        DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-28).AddDays(-1).ToString("yyyy-MM-dd");

    private static object R1(double weightKg = 62, string activityLevel = "moderatelyActive") => new
    {
        firstName = "Ana", lastName = "Pop", birthDate = R1BirthDate, heightCm = 168, weightKg,
        gender = "female", activityLevel, goal = "loseWeight"
    };

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> SignedInClientAsync(string email = "ana@exemplu.ro")
    {
        var client = factory.CreateApiClient();
        var register = await client.RegisterAsync(email);
        client.UseBearer(await register.ReadAccessTokenAsync());
        return client;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private static JsonElement Target(JsonElement profile, string name) => profile.GetProperty("targets").GetProperty(name);

    [Fact]
    public async Task GetProfile_AfterRegister_ReturnsDefaults()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.GetAsync("/api/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await JsonAsync(response);
        profile.GetProperty("isProfileComplete").GetBoolean().Should().BeFalse();
        profile.GetProperty("bmr").ValueKind.Should().Be(JsonValueKind.Null);
        Target(profile, "calories").GetProperty("value").GetDouble().Should().Be(2000);
        Target(profile, "calories").GetProperty("source").GetString().Should().Be("default");
    }

    [Fact]
    public async Task UpdateProfile_R1_ReturnsCalculatedTargets()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.PutAsJsonAsync("/api/profile", R1());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await JsonAsync(response);
        profile.GetProperty("bmr").GetInt32().Should().Be(1369);
        profile.GetProperty("tdee").GetInt32().Should().Be(2122);
        profile.GetProperty("gender").GetString().Should().Be("female");
        profile.GetProperty("goal").GetString().Should().Be("loseWeight");
        Target(profile, "calories").GetProperty("value").GetDouble().Should().Be(1700);
        Target(profile, "calories").GetProperty("source").GetString().Should().Be("calculated");
        Target(profile, "carbsGrams").GetProperty("value").GetDouble().Should().Be(195);
    }

    [Fact]
    public async Task UpdateProfile_MakesAuthMeProfileComplete()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        await client.PutAsJsonAsync("/api/profile", R1());
        var me = await JsonAsync(await client.GetAsync("/api/auth/me"));

        // Assert
        me.GetProperty("isProfileComplete").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTargets_ManualProteins_RecalculatesCarbs()
    {
        // Arrange
        var client = await SignedInClientAsync();
        await client.PutAsJsonAsync("/api/profile", R1());

        // Act
        var manual = await JsonAsync(await client.PutAsJsonAsync("/api/profile/targets", new { proteinsGrams = 140 }));
        var restored = await JsonAsync(await client.PutAsJsonAsync("/api/profile/targets", new { proteinsGrams = (double?)null }));

        // Assert
        Target(manual, "proteinsGrams").GetProperty("source").GetString().Should().Be("manual");
        Target(manual, "carbsGrams").GetProperty("value").GetDouble().Should().Be(179);
        Target(restored, "proteinsGrams").GetProperty("source").GetString().Should().Be("calculated");
        Target(restored, "proteinsGrams").GetProperty("value").GetDouble().Should().Be(124);
    }

    [Fact]
    public async Task UpdateProfile_KeepsManualTargets()
    {
        // Arrange
        var client = await SignedInClientAsync();
        await client.PutAsJsonAsync("/api/profile", R1());
        await client.PutAsJsonAsync("/api/profile/targets", new { proteinsGrams = 140 });

        // Act
        var profile = await JsonAsync(await client.PutAsJsonAsync("/api/profile", R1(weightKg: 70)));

        // Assert
        Target(profile, "proteinsGrams").GetProperty("value").GetDouble().Should().Be(140);
        Target(profile, "proteinsGrams").GetProperty("source").GetString().Should().Be("manual");
        profile.GetProperty("weightKg").GetDouble().Should().Be(70);
    }

    public static TheoryData<string> InvalidBodies => new()
    {
        """{"firstName":"Ana","lastName":"Pop","birthDate":"1998-05-14","heightCm":99,"weightKg":62,"gender":"female","activityLevel":"sedentary","goal":"loseWeight"}""",
        """{"firstName":"Ana","lastName":"Pop","birthDate":"1998-05-14","heightCm":168,"weightKg":301,"gender":"female","activityLevel":"sedentary","goal":"loseWeight"}""",
        """{"firstName":"Ana","lastName":"Pop","birthDate":"1998-05-14","heightCm":168,"weightKg":62,"gender":"alien","activityLevel":"sedentary","goal":"loseWeight"}""",
        """{"firstName":"Ana","lastName":"Pop","birthDate":"2015-01-01","heightCm":168,"weightKg":62,"gender":"female","activityLevel":"sedentary","goal":"loseWeight"}"""
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task UpdateProfile_InvalidValues_Returns400(string body)
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.PutAsync("/api/profile", new StringContent(body, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdateProfile_InvalidBirthDateFormat_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();
        const string body = """{"firstName":"Ana","lastName":"Pop","birthDate":"14-05-1998","heightCm":168,"weightKg":62,"gender":"female","activityLevel":"sedentary","goal":"loseWeight"}""";

        // Act
        var response = await client.PutAsync("/api/profile", new StringContent(body, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_OutOfRangeNumericEnum_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();
        const string body = """{"firstName":"Ana","lastName":"Pop","birthDate":"1998-05-14","heightCm":168,"weightKg":62,"gender":9,"activityLevel":"sedentary","goal":"loseWeight"}""";

        // Act
        var response = await client.PutAsync("/api/profile", new StringContent(body, Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTargets_Impossible_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();
        await client.PutAsJsonAsync("/api/profile", R1());

        // Act
        var response = await client.PutAsJsonAsync("/api/profile/targets", new { calories = 1000, proteinsGrams = 300 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await JsonAsync(response)).GetProperty("errors").GetProperty("Targets").GetArrayLength().Should().Be(1);
    }

    [Theory]
    [InlineData("GET", "/api/profile")]
    [InlineData("PUT", "/api/profile")]
    [InlineData("PUT", "/api/profile/targets")]
    public async Task Profile_WithoutToken_Returns401(string method, string url)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "PUT" ? JsonContent.Create(R1()) : null
        };

        // Act
        var response = await factory.CreateApiClient().SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_TwoUsers_SeeOnlyTheirOwn()
    {
        // Arrange
        var ana = await SignedInClientAsync("ana@exemplu.ro");
        var ion = await SignedInClientAsync("ion@exemplu.ro");
        await ana.PutAsJsonAsync("/api/profile", R1());

        // Act
        var ionProfile = await JsonAsync(await ion.GetAsync("/api/profile"));

        // Assert
        ionProfile.GetProperty("email").GetString().Should().Be("ion@exemplu.ro");
        ionProfile.GetProperty("isProfileComplete").GetBoolean().Should().BeFalse();
    }
}
