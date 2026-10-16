using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NutriScan.WebApi.IntegrationTests.Auth;

namespace NutriScan.WebApi.IntegrationTests.Diary;

[Collection(IntegrationTestCollection.Name)]
public class DiaryTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    // The API uses the real clock, so dates are relative to the day the suite runs.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static string Day(DateOnly date) => date.ToString("yyyy-MM-dd");

    // 389 kcal, 16.9 P, 66.3 C, 6.9 F per 100 g; one serving is 40 g.
    private static object Oats(double calories = 389) => new
    {
        name = "Fulgi de ovăz bio", brand = "Ferma Mea", servingSizeGrams = 40, calories, proteins = 16.9,
        carbohydrates = 66.3, fats = 6.9
    };

    private static object R1() => new
    {
        firstName = "Ana", lastName = "Pop",
        birthDate = Today.AddYears(-28).AddDays(-1).ToString("yyyy-MM-dd"),
        heightCm = 168, weightKg = 62, gender = "female", activityLevel = "moderatelyActive", goal = "loseWeight"
    };

    private static object Entry(string foodId, double? quantityGrams = 100, double? servings = null,
        string mealType = "breakfast", DateOnly? date = null) =>
        new { foodId, date = Day(date ?? Today), mealType, quantityGrams, servings };

    private static object Edit(double? quantityGrams = 100, double? servings = null, string mealType = "breakfast", DateOnly? date = null) =>
        new { date = Day(date ?? Today), mealType, quantityGrams, servings };

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> SignedInClientAsync(string email = "ana@exemplu.ro")
    {
        var client = factory.CreateApiClient();
        client.UseBearer(await (await client.RegisterAsync(email)).ReadAccessTokenAsync());
        return client;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private static async Task<string> IdOfAsync(HttpResponseMessage response) =>
        (await JsonAsync(response)).GetProperty("id").GetString()!;

    private static async Task<string> CreateOatsAsync(HttpClient client, double calories = 389) =>
        await IdOfAsync(await client.PostAsJsonAsync("/api/foods", Oats(calories)));

    private static async Task<string> AddEntryAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/diary/entries", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await IdOfAsync(response);
    }

    private static async Task<JsonElement> GetDayAsync(HttpClient client, DateOnly date) =>
        await JsonAsync(await client.GetAsync($"/api/diary/days/{Day(date)}"));

    private static double Number(JsonElement element, string path) =>
        path.Split('.').Aggregate(element, (e, p) => e.GetProperty(p)).GetDouble();

    [Fact]
    public async Task Add_Grams_Returns201WithLocationAndSnapshot()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);

        // Act
        var response = await client.PostAsJsonAsync("/api/diary/entries", Entry(oatsId, quantityGrams: 200));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var entry = await JsonAsync(response);
        response.Headers.Location!.ToString().Should().EndWith($"/api/diary/entries/{entry.GetProperty("id").GetString()}");
        entry.GetProperty("kind").GetString().Should().Be("food");
        entry.GetProperty("mealType").GetString().Should().Be("breakfast");
        Number(entry, "nutrients.calories").Should().Be(778);
        Number(entry, "nutrients.proteins").Should().Be(33.8);
        (await client.GetAsync(response.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Add_Servings_ConvertsToGrams()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);

        // Act
        var response = await client.PostAsJsonAsync("/api/diary/entries", Entry(oatsId, quantityGrams: null, servings: 1.5));

        // Assert
        var entry = await JsonAsync(response);
        Number(entry, "quantityGrams").Should().Be(60);
        Number(entry, "servings").Should().Be(1.5);
        Number(entry, "nutrients.calories").Should().Be(233.4);
    }

    [Fact]
    public async Task Add_ServingsForSeededOats_Returns400()
    {
        // Arrange: the seeded oats have no serving size
        var client = await SignedInClientAsync();
        var search = await JsonAsync(await client.GetAsync("/api/foods?query=fulgi%20ovaz%20integrali"));
        var seededId = search.GetProperty("items")[0].GetProperty("id").GetString()!;

        // Act
        var response = await client.PostAsJsonAsync("/api/diary/entries", Entry(seededId, quantityGrams: null, servings: 1));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDay_ReturnsFourMealsTotalsAndTargets()
    {
        // Arrange
        var client = await SignedInClientAsync();
        (await client.PutAsJsonAsync("/api/profile", R1())).EnsureSuccessStatusCode();
        var oatsId = await CreateOatsAsync(client);
        await AddEntryAsync(client, Entry(oatsId, quantityGrams: 100));
        await AddEntryAsync(client, Entry(oatsId, quantityGrams: 50, mealType: "snack"));

        // Act
        var day = await GetDayAsync(client, Today);

        // Assert
        day.GetProperty("meals").EnumerateArray().Select(m => m.GetProperty("mealType").GetString())
            .Should().Equal("breakfast", "lunch", "dinner", "snack");
        Number(day.GetProperty("meals")[3], "totals.calories").Should().Be(194.5);
        Number(day, "totals.calories").Should().Be(583.5);
        Number(day, "targets.calories").Should().Be(1700);
        Number(day, "remaining.calories").Should().Be(1116.5);
        day.GetProperty("isProfileComplete").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task EditFood_DoesNotChangeExistingEntry()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);
        var entryId = await AddEntryAsync(client, Entry(oatsId, quantityGrams: 100));

        // Act
        (await client.PutAsJsonAsync($"/api/foods/{oatsId}", Oats(calories: 400))).EnsureSuccessStatusCode();

        // Assert
        var entry = await JsonAsync(await client.GetAsync($"/api/diary/entries/{entryId}"));
        Number(entry, "nutrients.calories").Should().Be(389);
    }

    [Fact]
    public async Task Update_ChangesQuantityMealAndDate_Returns200()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);
        var entryId = await AddEntryAsync(client, Entry(oatsId));

        // Act
        var response = await client.PutAsJsonAsync($"/api/diary/entries/{entryId}",
            Edit(quantityGrams: null, servings: 2, mealType: "dinner", date: Today.AddDays(-1)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = await JsonAsync(response);
        Number(entry, "quantityGrams").Should().Be(80);
        entry.GetProperty("mealType").GetString().Should().Be("dinner");
        entry.GetProperty("date").GetString().Should().Be(Day(Today.AddDays(-1)));
    }

    [Fact]
    public async Task Update_MoveToAnotherDay_LeavesOldDay()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);
        var entryId = await AddEntryAsync(client, Entry(oatsId));

        // Act
        (await client.PutAsJsonAsync($"/api/diary/entries/{entryId}", Edit(date: Today.AddDays(-1)))).EnsureSuccessStatusCode();

        // Assert
        Number(await GetDayAsync(client, Today), "totals.calories").Should().Be(0);
        Number(await GetDayAsync(client, Today.AddDays(-1)), "totals.calories").Should().Be(389);
    }

    [Fact]
    public async Task Delete_Returns204ThenGet404()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);
        var entryId = await AddEntryAsync(client, Entry(oatsId));

        // Act
        var response = await client.DeleteAsync($"/api/diary/entries/{entryId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/diary/entries/{entryId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        Number(await GetDayAsync(client, Today), "totals.calories").Should().Be(0);
    }

    [Fact]
    public async Task OtherUsersEntry_Returns404ForGetPutDelete()
    {
        // Arrange
        var ana = await SignedInClientAsync();
        var entryId = await AddEntryAsync(ana, Entry(await CreateOatsAsync(ana)));
        var ion = await SignedInClientAsync("ion@exemplu.ro");

        // Act
        var get = await ion.GetAsync($"/api/diary/entries/{entryId}");
        var put = await ion.PutAsJsonAsync($"/api/diary/entries/{entryId}", Edit());
        var delete = await ion.DeleteAsync($"/api/diary/entries/{entryId}");

        // Assert
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        put.StatusCode.Should().Be(HttpStatusCode.NotFound);
        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Add_OtherUsersFood_Returns404()
    {
        // Arrange
        var ana = await SignedInClientAsync();
        var anasOats = await CreateOatsAsync(ana);
        var ion = await SignedInClientAsync("ion@exemplu.ro");

        // Act
        var response = await ion.PostAsJsonAsync("/api/diary/entries", Entry(anasOats));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ArchivedFoodEntry_StaysInDayAndIsEditable()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);
        var entryId = await AddEntryAsync(client, Entry(oatsId));
        (await client.DeleteAsync($"/api/foods/{oatsId}")).EnsureSuccessStatusCode();

        // Act
        var update = await client.PutAsJsonAsync($"/api/diary/entries/{entryId}", Edit(quantityGrams: 50));
        var day = await GetDayAsync(client, Today);

        // Assert
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var entry = day.GetProperty("meals")[0].GetProperty("entries")[0];
        entry.GetProperty("food").GetProperty("isArchived").GetBoolean().Should().BeTrue();
        Number(entry, "nutrients.calories").Should().Be(194.5);
        (await client.PostAsJsonAsync("/api/diary/entries", Entry(oatsId))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public static TheoryData<string> InvalidEntries => new()
    {
        """{"date":"{today}","mealType":"breakfast","quantityGrams":100,"servings":1}""",
        """{"date":"{today}","mealType":"breakfast"}""",
        """{"date":"{today}","mealType":"breakfast","quantityGrams":5001}""",
        """{"date":"{today}","mealType":"brunch","quantityGrams":100}""",
        """{"date":"{inTwoDays}","mealType":"breakfast","quantityGrams":100}"""
    };

    [Theory]
    [MemberData(nameof(InvalidEntries))]
    public async Task Add_InvalidRequest_Returns400(string template)
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);
        var json = template
            .Replace("{today}", Day(Today))
            .Replace("{inTwoDays}", Day(Today.AddDays(2)))
            .Replace("{\"date\"", $"{{\"foodId\":\"{oatsId}\",\"date\"");

        // Act
        var response = await client.PostAsync("/api/diary/entries", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("2026-13-01")]
    [InlineData("azi")]
    public async Task GetDay_MalformedDate_Returns400(string date)
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.GetAsync($"/api/diary/days/{date}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecentFoods_ReturnsDistinctNewestFirst()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateOatsAsync(client);
        var search = await JsonAsync(await client.GetAsync("/api/foods?query=broccoli"));
        var broccoliId = search.GetProperty("items")[0].GetProperty("id").GetString()!;
        await AddEntryAsync(client, Entry(oatsId, quantityGrams: 100));
        await AddEntryAsync(client, Entry(broccoliId, quantityGrams: 150));
        await AddEntryAsync(client, Entry(oatsId, quantityGrams: null, servings: 1.5));

        // Act
        var response = await client.GetAsync("/api/diary/recent-foods?limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recent = (await JsonAsync(response)).EnumerateArray().ToList();
        recent.Select(r => r.GetProperty("food").GetProperty("id").GetString()).Should().Equal(oatsId, broccoliId);
        Number(recent[0], "lastQuantityGrams").Should().Be(60);
        (await client.GetAsync("/api/diary/recent-foods?limit=31")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("GET", "/api/diary/days/2026-10-16")]
    [InlineData("GET", "/api/diary/recent-foods")]
    [InlineData("POST", "/api/diary/entries")]
    [InlineData("GET", "/api/diary/entries/00000000-0000-0000-0000-000000000001")]
    [InlineData("PUT", "/api/diary/entries/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/diary/entries/00000000-0000-0000-0000-000000000001")]
    public async Task Diary_WithoutToken_Returns401(string method, string url)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(Edit()) : null
        };

        // Act
        var response = await factory.CreateApiClient().SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
