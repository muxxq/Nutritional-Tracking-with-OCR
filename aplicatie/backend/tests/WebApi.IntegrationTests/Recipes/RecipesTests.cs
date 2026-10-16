using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NutriScan.Infrastructure.Persistence;
using NutriScan.WebApi.IntegrationTests.Auth;

namespace NutriScan.WebApi.IntegrationTests.Recipes;

[Collection(IntegrationTestCollection.Name)]
public class RecipesTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static string Day(DateOnly date) => date.ToString("yyyy-MM-dd");

    // 389 kcal, 16.9 P, 66.3 C, 6.9 F per 100 g.
    private static object Oats(double calories = 389) => new
    {
        name = "Fulgi de ovăz bio", brand = "Ferma Mea", servingSizeGrams = 40, calories, proteins = 16.9,
        carbohydrates = 66.3, fats = 6.9
    };

    private static object Chicken() => new
    {
        name = "Piept de pui", calories = 120, proteins = 22.5, carbohydrates = 0, fats = 2.6
    };

    private static object Recipe(string name = "Ovăz cu lapte", int servingsCount = 4, params (string FoodId, double Grams)[] ingredients) => new
    {
        name, description = (string?)null, instructions = "Se fierbe 5 minute.", servingsCount, prepTimeMinutes = 10,
        ingredients = ingredients.Select(i => new { foodId = i.FoodId, quantityGrams = i.Grams }).ToArray()
    };

    private static object RecipeEntry(string recipeId, double? servings = 1, double? quantityGrams = null) =>
        new { recipeId, date = Day(Today), mealType = "lunch", servings, quantityGrams };

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

    private static async Task<string> CreateRecipeAsync(HttpClient client, object recipe)
    {
        var response = await client.PostAsJsonAsync("/api/recipes", recipe);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await IdOfAsync(response);
    }

    private async Task<(HttpClient Client, string OatsId, string RecipeId)> ArrangeSrsRecipeAsync()
    {
        var client = await SignedInClientAsync();
        var oatsId = await CreateFoodAsync(client, Oats());
        var recipeId = await CreateRecipeAsync(client, Recipe(ingredients: (oatsId, 200)));
        return (client, oatsId, recipeId);
    }

    private static async Task<JsonElement> LunchEntriesAsync(HttpClient client) =>
        (await JsonAsync(await client.GetAsync($"/api/diary/days/{Day(Today)}")))
            .GetProperty("meals").EnumerateArray().Single(m => m.GetProperty("mealType").GetString() == "lunch")
            .GetProperty("entries");

    [Fact]
    public async Task Create_SrsExample_Returns201WithPerServing194Point5()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateFoodAsync(client, Oats());

        // Act
        var response = await client.PostAsJsonAsync("/api/recipes", Recipe(ingredients: (oatsId, 200)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var recipe = await JsonAsync(response);
        response.Headers.Location!.ToString().Should().EndWith($"/api/recipes/{recipe.GetProperty("id").GetString()}");
        Number(recipe, "total.calories").Should().Be(778);
        Number(recipe, "perServing.calories").Should().Be(194.5);
        Number(recipe, "perServing.proteins").Should().BeApproximately(8.5, 0.1);
        recipe.GetProperty("ingredients")[0].GetProperty("food").GetProperty("name").GetString().Should().Be("Fulgi de ovăz bio");
        (await client.GetAsync(response.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_ReplacesIngredients_Returns200()
    {
        // Arrange
        var (client, oatsId, recipeId) = await ArrangeSrsRecipeAsync();
        var chickenId = await CreateFoodAsync(client, Chicken());

        // Act
        var response = await client.PutAsJsonAsync($"/api/recipes/{recipeId}", Recipe("Bol", 2, (chickenId, 150)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recipe = await JsonAsync(await client.GetAsync($"/api/recipes/{recipeId}"));
        recipe.GetProperty("name").GetString().Should().Be("Bol");
        recipe.GetProperty("ingredients").GetArrayLength().Should().Be(1);
        Number(recipe, "total.calories").Should().Be(180);
        Number(recipe, "perServing.calories").Should().Be(90);
    }

    [Fact]
    public async Task Update_ChangesQuantityOfExistingIngredient_Returns200()
    {
        // Arrange
        var (client, oatsId, recipeId) = await ArrangeSrsRecipeAsync();
        var chickenId = await CreateFoodAsync(client, Chicken());

        // Act: the existing food comes second and with a new quantity; the unique (RecipeId, FoodItemId) index must hold
        var response = await client.PutAsJsonAsync($"/api/recipes/{recipeId}", Recipe(ingredients: [(chickenId, 150), (oatsId, 100)]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recipe = await JsonAsync(await client.GetAsync($"/api/recipes/{recipeId}"));
        recipe.GetProperty("ingredients").GetArrayLength().Should().Be(2);
        Number(recipe, "total.calories").Should().Be(569);
    }

    [Fact]
    public async Task Get_KeepsIngredientOrderOfLastSave()
    {
        // Arrange: four foods, so a random order matches the expected one by chance in 1 of 24 runs
        var client = await SignedInClientAsync();
        var ids = new List<string>();
        foreach (var name in new[] { "Ovăz", "Lapte", "Miere", "Nuci" })
        {
            ids.Add(await CreateFoodAsync(client, new { name, calories = 100, proteins = 1, carbohydrates = 1, fats = 1 }));
        }
        var recipeId = await CreateRecipeAsync(client, Recipe(ingredients: [(ids[0], 50), (ids[1], 200), (ids[2], 20), (ids[3], 30)]));

        // Act: reorder, keep every food
        var put = await client.PutAsJsonAsync($"/api/recipes/{recipeId}",
            Recipe(ingredients: [(ids[3], 30), (ids[1], 200), (ids[0], 50), (ids[2], 20)]));

        // Assert
        static IEnumerable<string> Order(JsonElement recipe) =>
            recipe.GetProperty("ingredients").EnumerateArray().Select(i => i.GetProperty("food").GetProperty("name").GetString()!);
        Order(await JsonAsync(put)).Should().Equal("Nuci", "Lapte", "Ovăz", "Miere");
        Order(await JsonAsync(await client.GetAsync($"/api/recipes/{recipeId}"))).Should().Equal("Nuci", "Lapte", "Ovăz", "Miere");
    }

    [Fact]
    public async Task EditFood_ChangesRecipeButNotLoggedEntry()
    {
        // Arrange
        var (client, _, recipeId) = await ArrangeSrsRecipeAsync();
        var oatsId = (await JsonAsync(await client.GetAsync($"/api/recipes/{recipeId}")))
            .GetProperty("ingredients")[0].GetProperty("food").GetProperty("id").GetString();
        (await client.PostAsJsonAsync("/api/diary/entries", RecipeEntry(recipeId))).StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        (await client.PutAsJsonAsync($"/api/foods/{oatsId}", Oats(calories: 400))).StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var recipe = await JsonAsync(await client.GetAsync($"/api/recipes/{recipeId}"));
        Number(recipe, "perServing.calories").Should().Be(200);
        Number((await LunchEntriesAsync(client))[0], "nutrients.calories").Should().Be(194.5);
    }

    [Fact]
    public async Task Delete_ArchivesAndKeepsDiaryEntry()
    {
        // Arrange
        var (client, _, recipeId) = await ArrangeSrsRecipeAsync();
        var entryId = await IdOfAsync(await client.PostAsJsonAsync("/api/diary/entries", RecipeEntry(recipeId)));

        // Act
        var response = await client.DeleteAsync($"/api/recipes/{recipeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/recipes/{recipeId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.DeleteAsync($"/api/recipes/{recipeId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        Number(await JsonAsync(await client.GetAsync("/api/recipes")), "totalCount").Should().Be(0);
        (await client.PostAsJsonAsync("/api/diary/entries", RecipeEntry(recipeId))).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var entry = (await LunchEntriesAsync(client)).EnumerateArray().Should().ContainSingle().Subject;
        entry.GetProperty("recipe").GetProperty("isArchived").GetBoolean().Should().BeTrue();

        var edit = await client.PutAsJsonAsync($"/api/diary/entries/{entryId}",
            new { date = Day(Today), mealType = "dinner", servings = 2 });
        edit.StatusCode.Should().Be(HttpStatusCode.OK);
        Number(await JsonAsync(edit), "nutrients.calories").Should().Be(389);
        (await client.DeleteAsync($"/api/diary/entries/{entryId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AddRecipeEntry_DayIncludesItInTotals()
    {
        // Arrange
        var (client, _, recipeId) = await ArrangeSrsRecipeAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/diary/entries", RecipeEntry(recipeId, servings: 1.5));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var entry = await JsonAsync(response);
        entry.GetProperty("kind").GetString().Should().Be("recipe");
        entry.GetProperty("quantityGrams").ValueKind.Should().Be(JsonValueKind.Null);
        entry.GetProperty("food").ValueKind.Should().Be(JsonValueKind.Null);
        entry.GetProperty("recipe").GetProperty("name").GetString().Should().Be("Ovăz cu lapte");
        Number(entry, "nutrients.calories").Should().Be(291.8);

        var day = await JsonAsync(await client.GetAsync($"/api/diary/days/{Day(Today)}"));
        Number(day, "totals.calories").Should().Be(291.8);
        (await client.GetAsync(response.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OtherUsersRecipe_Returns404Everywhere()
    {
        // Arrange
        var (_, oatsId, recipeId) = await ArrangeSrsRecipeAsync();
        var stranger = await SignedInClientAsync("mihai@exemplu.ro");
        var ownFood = await CreateFoodAsync(stranger, Oats());

        // Act
        var responses = new[]
        {
            await stranger.GetAsync($"/api/recipes/{recipeId}"),
            await stranger.PutAsJsonAsync($"/api/recipes/{recipeId}", Recipe(ingredients: (ownFood, 100))),
            await stranger.DeleteAsync($"/api/recipes/{recipeId}"),
            await stranger.PostAsJsonAsync("/api/diary/entries", RecipeEntry(recipeId))
        };

        // Assert
        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.NotFound);
        Number(await JsonAsync(await stranger.GetAsync("/api/recipes")), "totalCount").Should().Be(0);
    }

    [Fact]
    public async Task Create_OtherUsersFood_Returns404()
    {
        // Arrange
        var owner = await SignedInClientAsync();
        var oatsId = await CreateFoodAsync(owner, Oats());
        var stranger = await SignedInClientAsync("mihai@exemplu.ro");

        // Act
        var response = await stranger.PostAsJsonAsync("/api/recipes", Recipe(ingredients: (oatsId, 200)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public static TheoryData<string> InvalidBodies => new() { "duplicate", "noIngredients", "zeroServings" };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Create_InvalidBody_Returns400(string kind)
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateFoodAsync(client, Oats());
        var body = kind switch
        {
            "duplicate" => Recipe(ingredients: [(oatsId, 100), (oatsId, 50)]),
            "noIngredients" => Recipe(),
            _ => Recipe(servingsCount: 0, ingredients: (oatsId, 100))
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/recipes", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await JsonAsync(response)).GetProperty("errors").EnumerateObject().Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddEntry_FoodAndRecipe_Returns400()
    {
        // Arrange
        var (client, oatsId, recipeId) = await ArrangeSrsRecipeAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/diary/entries",
            new { foodId = oatsId, recipeId, date = Day(Today), mealType = "lunch", servings = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Alege fie un aliment, fie o rețetă.");
    }

    [Fact]
    public async Task AddEntry_RecipeWithGrams_Returns400()
    {
        // Arrange
        var (client, _, recipeId) = await ArrangeSrsRecipeAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/diary/entries", RecipeEntry(recipeId, servings: null, quantityGrams: 300));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Rețetele se adaugă în porții, nu în grame.");
    }

    [Fact]
    public async Task CheckConstraints_RejectDirectSqlInserts()
    {
        // Arrange
        var (_, oatsId, recipeId) = await ArrangeSrsRecipeAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = await db.Users.Select(u => u.Id).SingleAsync();
        const string insert = """
            INSERT INTO "DiaryEntries" ("Id", "UserId", "FoodItemId", "RecipeId", "ConsumedDate", "MealType",
                "QuantityGrams", "Servings", "CalculatedCalories", "CalculatedProteins", "CalculatedCarbs",
                "CalculatedFats", "CreatedAtUtc")
            VALUES ({0}, {1}, {2}, {3}, CURRENT_DATE, 0, {4}, {5}, 0, 0, 0, 0, now())
            """;

        // Act
        var both = () => db.Database.ExecuteSqlRawAsync(insert,
            Guid.NewGuid(), userId, Guid.Parse(oatsId), Guid.Parse(recipeId), 100.0, 1.0);
        var recipeWithGrams = () => db.Database.ExecuteSqlRawAsync(insert,
            Guid.NewGuid(), userId, new NpgsqlParameter { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid, Value = DBNull.Value },
            Guid.Parse(recipeId), 100.0, 1.0);

        // Assert
        (await both.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
        (await recipeWithGrams.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
    }

    [Fact]
    public async Task Recipes_WithoutToken_Return401()
    {
        // Arrange
        var client = factory.CreateApiClient();
        var id = Guid.NewGuid();

        // Act
        var responses = new[]
        {
            await client.GetAsync("/api/recipes"),
            await client.GetAsync($"/api/recipes/{id}"),
            await client.PostAsJsonAsync("/api/recipes", Recipe()),
            await client.PutAsJsonAsync($"/api/recipes/{id}", Recipe()),
            await client.DeleteAsync($"/api/recipes/{id}")
        };

        // Assert
        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_ByNameWithoutDiacritics_FindsRecipe()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var oatsId = await CreateFoodAsync(client, Oats());
        await CreateRecipeAsync(client, Recipe("Ciorbă de perișoare", ingredients: (oatsId, 100)));
        await CreateRecipeAsync(client, Recipe("Tocăniță", ingredients: (oatsId, 100)));

        // Act
        var result = await JsonAsync(await client.GetAsync("/api/recipes?q=ciorba"));

        // Assert
        var item = result.GetProperty("items").EnumerateArray().Should().ContainSingle().Subject;
        item.GetProperty("name").GetString().Should().Be("Ciorbă de perișoare");
        item.GetProperty("ingredientCount").GetInt32().Should().Be(1);
        Number(item, "perServing.calories").Should().Be(97.3);
    }
}
