using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using NutriScan.Infrastructure.Persistence;
using NutriScan.WebApi.IntegrationTests.Auth;

namespace NutriScan.WebApi.IntegrationTests.Planner;

[Collection(IntegrationTestCollection.Name)]
public class PlannerTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    // The API uses the real clock, so dates are relative to the day the suite runs.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static string Day(DateOnly date) => date.ToString("yyyy-MM-dd");

    // 389 kcal per 100 g; one serving is 40 g.
    private static object Oats(double calories = 389) => new
    {
        name = "Fulgi de ovăz bio", servingSizeGrams = 40, calories, proteins = 16.9, carbohydrates = 66.3, fats = 6.9
    };

    // 100 kcal per 100 g, so grams equal kcal.
    private static object Hundred() => new { name = "Test", calories = 100, proteins = 5, carbohydrates = 10, fats = 2 };

    private static object R1() => new
    {
        firstName = "Ana", lastName = "Pop",
        birthDate = Today.AddYears(-28).AddDays(-1).ToString("yyyy-MM-dd"),
        heightCm = 168, weightKg = 62, gender = "female", activityLevel = "moderatelyActive", goal = "loseWeight"
    };

    private static object FoodMeal(string foodId, double? quantityGrams = 100, double? servings = null, DateOnly? date = null,
        string mealType = "lunch") => new { foodId, date = Day(date ?? Today), mealType, quantityGrams, servings };

    private static object RecipeMeal(string recipeId, double? servings = 2, double? quantityGrams = null, DateOnly? date = null) =>
        new { recipeId, date = Day(date ?? Today), mealType = "lunch", servings, quantityGrams };

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

    private static double Number(JsonElement element, string path) =>
        path.Split('.').Aggregate(element, (e, p) => e.GetProperty(p)).GetDouble();

    private static async Task<string> CreateFoodAsync(HttpClient client, object food) =>
        await IdOfAsync(await client.PostAsJsonAsync("/api/foods", food));

    /// <summary>The SRS recipe: 200 g oats, 4 servings, 194.5 kcal per serving.</summary>
    private static async Task<string> CreateSrsRecipeAsync(HttpClient client, string oatsId) =>
        await IdOfAsync(await client.PostAsJsonAsync("/api/recipes", new
        {
            name = "Ovăz cu lapte", servingsCount = 4, ingredients = new[] { new { foodId = oatsId, quantityGrams = 200.0 } }
        }));

    private static async Task<string> PlanAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/plan/items", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await IdOfAsync(response);
    }

    private static async Task<JsonElement> GetPlanAsync(HttpClient client, DateOnly from, DateOnly to) =>
        await JsonAsync(await client.GetAsync($"/api/plan?from={Day(from)}&to={Day(to)}"));

    [Fact]
    public async Task Create_Recipe_Returns201WithComputedValues()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var recipeId = await CreateSrsRecipeAsync(client, await CreateFoodAsync(client, Oats()));

        // Act
        var response = await client.PostAsJsonAsync("/api/plan/items", RecipeMeal(recipeId, servings: 2));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var meal = await JsonAsync(response);
        response.Headers.Location!.ToString().Should().EndWith($"/api/plan/items/{meal.GetProperty("id").GetString()}");
        meal.GetProperty("kind").GetString().Should().Be("recipe");
        meal.GetProperty("quantityGrams").ValueKind.Should().Be(JsonValueKind.Null);
        Number(meal, "nutrients.calories").Should().Be(389);
        meal.GetProperty("diaryEntryId").ValueKind.Should().Be(JsonValueKind.Null);
        (await client.GetAsync(response.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPlan_WeekWithStatus()
    {
        // Arrange: R1 targets 1700 kcal
        var client = await SignedInClientAsync();
        (await client.PutAsJsonAsync("/api/profile", R1())).EnsureSuccessStatusCode();
        var foodId = await CreateFoodAsync(client, Hundred());
        await PlanAsync(client, FoodMeal(foodId, 600, date: Today.AddDays(1), mealType: "breakfast"));
        await PlanAsync(client, FoodMeal(foodId, 1050, date: Today.AddDays(1), mealType: "dinner"));

        // Act
        var plan = await GetPlanAsync(client, Today, Today.AddDays(6));

        // Assert
        var days = plan.GetProperty("days");
        days.GetArrayLength().Should().Be(7);
        days[0].GetProperty("status").GetString().Should().Be("under");
        days[0].GetProperty("meals").GetArrayLength().Should().Be(4);
        Number(days[1], "totals.calories").Should().Be(1650);
        Number(days[1], "targets.calories").Should().Be(1700);
        days[1].GetProperty("status").GetString().Should().Be("within");
        plan.GetProperty("isProfileComplete").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task EditFood_ChangesPlan()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var foodId = await CreateFoodAsync(client, Oats());
        await PlanAsync(client, FoodMeal(foodId, 100));

        // Act
        (await client.PutAsJsonAsync($"/api/foods/{foodId}", Oats(calories: 400))).EnsureSuccessStatusCode();

        // Assert
        Number((await GetPlanAsync(client, Today, Today)).GetProperty("days")[0], "totals.calories").Should().Be(400);
    }

    [Fact]
    public async Task Eat_CreatesDiaryEntry_Returns201()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var recipeId = await CreateSrsRecipeAsync(client, await CreateFoodAsync(client, Oats()));
        var mealId = await PlanAsync(client, RecipeMeal(recipeId, servings: 2));

        // Act
        var response = await client.PostAsync($"/api/plan/items/{mealId}/eat", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var entry = await JsonAsync(response);
        var entryId = entry.GetProperty("id").GetString();
        response.Headers.Location!.ToString().Should().EndWith($"/api/diary/entries/{entryId}");
        entry.GetProperty("kind").GetString().Should().Be("recipe");
        Number(entry, "servings").Should().Be(2);
        Number(entry, "nutrients.calories").Should().Be(389);
        var day = await JsonAsync(await client.GetAsync($"/api/diary/days/{Day(Today)}"));
        Number(day, "totals.calories").Should().Be(389);
        var meal = await JsonAsync(await client.GetAsync($"/api/plan/items/{mealId}"));
        meal.GetProperty("diaryEntryId").GetString().Should().Be(entryId);
    }

    [Fact]
    public async Task Eat_Twice_Returns409()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var mealId = await PlanAsync(client, FoodMeal(await CreateFoodAsync(client, Oats())));
        (await client.PostAsync($"/api/plan/items/{mealId}/eat", null)).StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await client.PostAsync($"/api/plan/items/{mealId}/eat", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Eat_AfterDiaryEntryDeleted_Returns201()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var mealId = await PlanAsync(client, FoodMeal(await CreateFoodAsync(client, Oats())));
        var entryId = await IdOfAsync(await client.PostAsync($"/api/plan/items/{mealId}/eat", null));
        (await client.DeleteAsync($"/api/diary/entries/{entryId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var response = await client.PostAsync($"/api/plan/items/{mealId}/eat", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Eat_FutureMeal_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var mealId = await PlanAsync(client, FoodMeal(await CreateFoodAsync(client, Oats()), date: Today.AddDays(2)));

        // Act
        var response = await client.PostAsync($"/api/plan/items/{mealId}/eat", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("după ziua de mâine");
    }

    [Fact]
    public async Task ArchivedRecipe_StaysInPlanAndCanBeEaten()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var recipeId = await CreateSrsRecipeAsync(client, await CreateFoodAsync(client, Oats()));
        var mealId = await PlanAsync(client, RecipeMeal(recipeId));
        (await client.DeleteAsync($"/api/recipes/{recipeId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var plan = await GetPlanAsync(client, Today, Today);
        var eat = await client.PostAsync($"/api/plan/items/{mealId}/eat", null);

        // Assert
        var item = plan.GetProperty("days")[0].GetProperty("meals").EnumerateArray()
            .SelectMany(m => m.GetProperty("items").EnumerateArray()).Should().ContainSingle().Subject;
        item.GetProperty("recipe").GetProperty("isArchived").GetBoolean().Should().BeTrue();
        eat.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/plan/items", RecipeMeal(recipeId))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ArchivedFood_Returns404()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var foodId = await CreateFoodAsync(client, Oats());
        (await client.DeleteAsync($"/api/foods/{foodId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var response = await client.PostAsJsonAsync("/api/plan/items", FoodMeal(foodId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OtherUsersMeal_Returns404Everywhere()
    {
        // Arrange
        var owner = await SignedInClientAsync();
        var mealId = await PlanAsync(owner, FoodMeal(await CreateFoodAsync(owner, Oats())));
        var stranger = await SignedInClientAsync("mihai@exemplu.ro");

        // Act
        var responses = new[]
        {
            await stranger.GetAsync($"/api/plan/items/{mealId}"),
            await stranger.PutAsJsonAsync($"/api/plan/items/{mealId}", new { date = Day(Today), mealType = "lunch", quantityGrams = 50 }),
            await stranger.DeleteAsync($"/api/plan/items/{mealId}"),
            await stranger.PostAsync($"/api/plan/items/{mealId}/eat", null)
        };

        // Assert
        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.NotFound);
        Number((await GetPlanAsync(stranger, Today, Today)).GetProperty("days")[0], "totals.calories").Should().Be(0);
    }

    public static TheoryData<string> InvalidBodies => new() { "bothIds", "recipeWithGrams", "zeroServings" };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Create_InvalidBody_Returns400(string kind)
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateFoodAsync(client, Oats());
        var recipeId = await CreateSrsRecipeAsync(client, oatsId);
        object body = kind switch
        {
            "bothIds" => new { foodId = oatsId, recipeId, date = Day(Today), mealType = "lunch", servings = 1 },
            "recipeWithGrams" => RecipeMeal(recipeId, servings: null, quantityGrams: 300),
            _ => RecipeMeal(recipeId, servings: 0)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/plan/items", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPlan_Range32Days_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.GetAsync($"/api/plan?from={Day(Today)}&to={Day(Today.AddDays(31))}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckConstraints_RejectDirectSqlInserts()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = Guid.Parse(await CreateFoodAsync(client, Oats()));
        var recipeId = Guid.Parse(await CreateSrsRecipeAsync(client, oatsId.ToString()));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = await db.Users.Select(u => u.Id).SingleAsync();
        const string insert = """
            INSERT INTO "PlannedMeals" ("Id", "UserId", "FoodItemId", "RecipeId", "PlanDate", "MealType",
                "QuantityGrams", "Servings", "CreatedAtUtc")
            VALUES ({0}, {1}, {2}, {3}, CURRENT_DATE, 0, {4}, {5}, now())
            """;
        static NpgsqlParameter Null(NpgsqlDbType type) => new() { NpgsqlDbType = type, Value = DBNull.Value };
        var rows = new (object Food, object Recipe, object Grams, object Servings)[]
        {
            (oatsId, recipeId, 100.0, 1.0),                                                     // both keys
            (Null(NpgsqlDbType.Uuid), Null(NpgsqlDbType.Uuid), 100.0, 1.0),                     // neither key
            (Null(NpgsqlDbType.Uuid), recipeId, 100.0, 1.0),                                    // recipe with grams
            (Null(NpgsqlDbType.Uuid), recipeId, Null(NpgsqlDbType.Double), Null(NpgsqlDbType.Double))  // recipe without servings
        };

        foreach (var row in rows)
        {
            // Act
            var act = () => db.Database.ExecuteSqlRawAsync(insert, Guid.NewGuid(), userId, row.Food, row.Recipe, row.Grams, row.Servings);

            // Assert
            (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
        }
    }

    [Fact]
    public async Task Plan_WithoutToken_Return401()
    {
        // Arrange
        var client = factory.CreateApiClient();
        var id = Guid.NewGuid();

        // Act
        var responses = new[]
        {
            await client.GetAsync($"/api/plan?from={Day(Today)}&to={Day(Today)}"),
            await client.GetAsync($"/api/plan/items/{id}"),
            await client.PostAsJsonAsync("/api/plan/items", new { }),
            await client.PutAsJsonAsync($"/api/plan/items/{id}", new { }),
            await client.DeleteAsync($"/api/plan/items/{id}"),
            await client.PostAsync($"/api/plan/items/{id}/eat", null)
        };

        // Assert
        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
    }
}
