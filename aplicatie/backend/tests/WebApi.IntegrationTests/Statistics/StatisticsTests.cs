using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NutriScan.WebApi.IntegrationTests.Auth;

namespace NutriScan.WebApi.IntegrationTests.Statistics;

[Collection(IntegrationTestCollection.Name)]
public class StatisticsTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    // The API uses the real clock, so dates are relative to the day the suite runs.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly DateOnly Start = Today.AddDays(-6);

    private static string Day(DateOnly date) => date.ToString("yyyy-MM-dd");

    // Target 1700 kcal.
    private static object R1() => new
    {
        firstName = "Ana", lastName = "Pop",
        birthDate = Today.AddYears(-28).AddDays(-1).ToString("yyyy-MM-dd"),
        heightCm = 168, weightKg = 62, gender = "female", activityLevel = "moderatelyActive", goal = "loseWeight"
    };

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> SignedInClientAsync(string email = "ana@exemplu.ro")
    {
        var client = factory.CreateApiClient();
        client.UseBearer(await (await client.RegisterAsync(email)).ReadAccessTokenAsync());
        (await client.PutAsJsonAsync("/api/profile", R1())).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    /// <summary>A food with 100 kcal per 100 g, so grams equal kcal.</summary>
    private static async Task<string> CreateFoodAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/foods",
            new { name = "Test", calories = 100, proteins = 5, carbohydrates = 10, fats = 2 });
        return (await JsonAsync(response)).GetProperty("id").GetString()!;
    }

    private static async Task LogAsync(HttpClient client, string foodId, DateOnly date, double grams, string mealType = "lunch") =>
        (await client.PostAsJsonAsync("/api/diary/entries", new { foodId, date = Day(date), mealType, quantityGrams = grams }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

    private static Task<HttpResponseMessage> GetStatsAsync(HttpClient client, DateOnly from, DateOnly to) =>
        client.GetAsync($"/api/stats?from={Day(from)}&to={Day(to)}");

    [Fact]
    public async Task Week_AveragesLoggedDaysAndCountsStatuses()
    {
        // Arrange: 1500 kcal (two meals), 1700 and 1900 on three of seven days
        var client = await SignedInClientAsync();
        var food = await CreateFoodAsync(client);
        await LogAsync(client, food, Start, 1000, "breakfast");
        await LogAsync(client, food, Start, 500);
        await LogAsync(client, food, Start.AddDays(2), 1700);
        await LogAsync(client, food, Start.AddDays(4), 1900);

        // Act
        var response = await GetStatsAsync(client, Start, Today);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonAsync(response);
        json.GetProperty("totalDays").GetInt32().Should().Be(7);
        json.GetProperty("loggedDays").GetInt32().Should().Be(3);
        json.GetProperty("average").GetProperty("calories").GetDouble().Should().Be(1700);
        json.GetProperty("deviation").GetProperty("calories").GetDouble().Should().Be(0);
        json.GetProperty("caloriesDeviationPercent").GetDouble().Should().Be(0);
        var counts = json.GetProperty("statusCounts");
        (counts.GetProperty("under").GetInt32(), counts.GetProperty("within").GetInt32(), counts.GetProperty("over").GetInt32())
            .Should().Be((1, 1, 1));

        var days = json.GetProperty("days").EnumerateArray().ToList();
        days.Should().HaveCount(7);
        days[0].GetProperty("totals").GetProperty("calories").GetDouble().Should().Be(1500);
        days[0].GetProperty("status").GetString().Should().Be("under");
        days[1].GetProperty("isLogged").GetBoolean().Should().BeFalse();
        days[1].GetProperty("status").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task NothingLogged_Returns200WithNulls()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await GetStatsAsync(client, Today.AddDays(-29), Today);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await JsonAsync(response);
        json.GetProperty("loggedDays").GetInt32().Should().Be(0);
        json.GetProperty("average").ValueKind.Should().Be(JsonValueKind.Null);
        json.GetProperty("deviation").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task OtherUsersEntries_AreNotCounted()
    {
        // Arrange
        var ana = await SignedInClientAsync();
        await LogAsync(ana, await CreateFoodAsync(ana), Today, 800);
        var ion = await SignedInClientAsync("ion@exemplu.ro");

        // Act
        var json = await JsonAsync(await GetStatsAsync(ion, Today, Today));

        // Assert
        json.GetProperty("loggedDays").GetInt32().Should().Be(0);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-366, 0)]
    public async Task InvalidRange_Returns400(int fromOffset, int toOffset)
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await GetStatsAsync(client, Today.AddDays(fromOffset), Today.AddDays(toOffset));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WithoutToken_Returns401()
    {
        // Act
        var response = await GetStatsAsync(factory.CreateApiClient(), Today, Today);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
