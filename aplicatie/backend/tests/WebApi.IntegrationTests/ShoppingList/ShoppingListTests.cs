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

namespace NutriScan.WebApi.IntegrationTests.ShoppingList;

[Collection(IntegrationTestCollection.Name)]
public class ShoppingListTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    // The API uses the real clock, so dates are relative to the day the suite runs.
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static string Day(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static object Food(string name, string category) =>
        new { name, calories = 100, proteins = 10, carbohydrates = 10, fats = 1, category };

    private static object Week(int offset = 0) => new { from = Day(Today.AddDays(offset)), to = Day(Today.AddDays(offset + 6)) };

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

    private static async Task<string> CreateFoodAsync(HttpClient client, string name, string category) =>
        await IdOfAsync(await client.PostAsJsonAsync("/api/foods", Food(name, category)));

    private static async Task<string> CreateRecipeAsync(HttpClient client, int servingsCount, string foodId, double grams) =>
        await IdOfAsync(await client.PostAsJsonAsync("/api/recipes", new
        {
            name = $"Rețetă {Guid.NewGuid():N}", servingsCount, ingredients = new[] { new { foodId, quantityGrams = grams } }
        }));

    private static async Task<string> PlanRecipeAsync(HttpClient client, string recipeId, double servings) =>
        await IdOfAsync(await client.PostAsJsonAsync("/api/plan/items",
            new { recipeId, date = Day(Today), mealType = "lunch", servings }));

    private static async Task<string> PlanFoodAsync(HttpClient client, string foodId, double quantityGrams) =>
        await IdOfAsync(await client.PostAsJsonAsync("/api/plan/items",
            new { foodId, date = Day(Today), mealType = "dinner", quantityGrams }));

    private static async Task<JsonElement> GenerateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/shopping-list/generate", Week());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await JsonAsync(response);
    }

    private static IEnumerable<JsonElement> Items(JsonElement list) =>
        list.GetProperty("categories").EnumerateArray().SelectMany(c => c.GetProperty("items").EnumerateArray());

    [Fact]
    public async Task Generate_SrsExample_Returns400gChicken()
    {
        // Arrange: FR-12 acceptance criterion
        var client = await SignedInClientAsync();
        var chicken = await CreateFoodAsync(client, "Piept de pui", "meatFish");
        await PlanRecipeAsync(client, await CreateRecipeAsync(client, 1, chicken, 150), 1);
        await PlanRecipeAsync(client, await CreateRecipeAsync(client, 1, chicken, 250), 1);

        // Act
        var list = await GenerateAsync(client);

        // Assert
        var item = Items(list).Should().ContainSingle().Subject;
        item.GetProperty("name").GetString().Should().Be("Piept de pui");
        item.GetProperty("quantityGrams").GetDouble().Should().Be(400);
        item.GetProperty("category").GetString().Should().Be("meatFish");
        item.GetProperty("isManual").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Generate_ScalesRecipeServings()
    {
        // Arrange: 200 g for 4 servings, 2 planned → 100 g
        var client = await SignedInClientAsync();
        var oats = await CreateFoodAsync(client, "Ovăz", "grains");
        await PlanRecipeAsync(client, await CreateRecipeAsync(client, 4, oats, 200), 2);

        // Act
        var list = await GenerateAsync(client);

        // Assert
        Items(list).Single().GetProperty("quantityGrams").GetDouble().Should().Be(100);
    }

    [Fact]
    public async Task Generate_GroupsByCategory()
    {
        // Arrange
        var client = await SignedInClientAsync();
        await PlanFoodAsync(client, await CreateFoodAsync(client, "Piept de pui", "meatFish"), 100);
        await PlanFoodAsync(client, await CreateFoodAsync(client, "Ovăz", "grains"), 100);
        await PlanFoodAsync(client, await CreateFoodAsync(client, "Banană", "fruitsVegetables"), 100);

        // Act
        var list = await GenerateAsync(client);

        // Assert
        list.GetProperty("categories").EnumerateArray().Select(c => c.GetProperty("category").GetString())
            .Should().Equal("fruitsVegetables", "meatFish", "grains");
        list.GetProperty("totalCount").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Generate_KeepsManualAndSkipsEaten()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var eaten = await PlanFoodAsync(client, await CreateFoodAsync(client, "Ovăz", "grains"), 100);
        await PlanFoodAsync(client, await CreateFoodAsync(client, "Orez", "grains"), 200);
        (await client.PostAsync($"/api/plan/items/{eaten}/eat", null)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/shopping-list/items", new { name = "Hârtie de copt" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        await GenerateAsync(client);
        var list = await GenerateAsync(client);

        // Assert
        Items(list).Select(i => i.GetProperty("name").GetString()).Should().BeEquivalentTo("Orez", "Hârtie de copt");
    }

    [Fact]
    public async Task Generate_EmptyRange_Returns400AndKeepsList()
    {
        // Arrange
        var client = await SignedInClientAsync();
        await PlanFoodAsync(client, await CreateFoodAsync(client, "Ovăz", "grains"), 100);
        await GenerateAsync(client);

        // Act
        var response = await client.PostAsJsonAsync("/api/shopping-list/generate", Week(offset: 7));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Planifică mai întâi mesele");
        Items(await JsonAsync(await client.GetAsync("/api/shopping-list"))).Should().ContainSingle();
    }

    [Fact]
    public async Task Items_AddCheckDeleteAndClearPurchased()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var add = await client.PostAsJsonAsync("/api/shopping-list/items", new { name = "Sare", quantity = "1 kg", category = "pantry" });
        var salt = await IdOfAsync(add);
        var paper = await IdOfAsync(await client.PostAsJsonAsync("/api/shopping-list/items", new { name = "Hârtie de copt" }));
        var milk = await IdOfAsync(await client.PostAsJsonAsync("/api/shopping-list/items", new { name = "Lapte", category = "dairy" }));

        // Act
        var check = await client.PatchAsJsonAsync($"/api/shopping-list/items/{salt}", new { isPurchased = true });
        var delete = await client.DeleteAsync($"/api/shopping-list/items/{paper}");
        var clear = await client.DeleteAsync("/api/shopping-list/purchased");

        // Assert
        add.StatusCode.Should().Be(HttpStatusCode.Created);
        (await JsonAsync(add)).GetProperty("quantityText").GetString().Should().Be("1 kg");
        check.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(check)).GetProperty("isPurchased").GetBoolean().Should().BeTrue();
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        clear.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Items(await JsonAsync(await client.GetAsync("/api/shopping-list"))).Select(i => i.GetProperty("id").GetString())
            .Should().Equal(milk);
    }

    [Fact]
    public async Task SetPurchased_MissingValue_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var id = await IdOfAsync(await client.PostAsJsonAsync("/api/shopping-list/items", new { name = "Sare" }));

        // Act
        var response = await client.PatchAsJsonAsync($"/api/shopping-list/items/{id}", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OtherUsersItem_Returns404()
    {
        // Arrange
        var owner = await SignedInClientAsync();
        var id = await IdOfAsync(await owner.PostAsJsonAsync("/api/shopping-list/items", new { name = "Sare" }));
        var stranger = await SignedInClientAsync("mihai@exemplu.ro");

        // Act
        var responses = new[]
        {
            await stranger.PatchAsJsonAsync($"/api/shopping-list/items/{id}", new { isPurchased = true }),
            await stranger.DeleteAsync($"/api/shopping-list/items/{id}")
        };

        // Assert
        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.NotFound);
        Items(await JsonAsync(await stranger.GetAsync("/api/shopping-list"))).Should().BeEmpty();
    }

    [Fact]
    public async Task AddItem_BlankName_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/shopping-list/items", new { name = "  " });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShoppingList_WithoutToken_Return401()
    {
        // Arrange
        var client = factory.CreateApiClient();
        var id = Guid.NewGuid();

        // Act
        var responses = new[]
        {
            await client.GetAsync("/api/shopping-list"),
            await client.PostAsJsonAsync("/api/shopping-list/generate", Week()),
            await client.PostAsJsonAsync("/api/shopping-list/items", new { name = "Sare" }),
            await client.PatchAsJsonAsync($"/api/shopping-list/items/{id}", new { isPurchased = true }),
            await client.DeleteAsync($"/api/shopping-list/items/{id}"),
            await client.DeleteAsync("/api/shopping-list/purchased")
        };

        // Assert
        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CheckConstraint_RejectsDirectSqlInserts()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var foodId = Guid.Parse(await CreateFoodAsync(client, "Ovăz", "grains"));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = await db.Users.Select(u => u.Id).SingleAsync();
        const string insert = """
            INSERT INTO "ShoppingListItems" ("Id", "UserId", "FoodItemId", "ItemName", "QuantityGrams", "QuantityText",
                "Category", "IsPurchased", "CreatedAtUtc")
            VALUES ({0}, {1}, {2}, 'X', {3}, {4}, 0, false, now())
            """;
        static NpgsqlParameter Null(NpgsqlDbType type) => new() { NpgsqlDbType = type, Value = DBNull.Value };
        var rows = new (object Food, object Grams, object Text)[]
        {
            (foodId, Null(NpgsqlDbType.Double), Null(NpgsqlDbType.Varchar)),   // generated without grams
            (foodId, 100.0, "1 kg"),                                             // generated with text
            (Null(NpgsqlDbType.Uuid), 100.0, Null(NpgsqlDbType.Varchar))        // manual with grams
        };

        foreach (var row in rows)
        {
            // Act
            var act = () => db.Database.ExecuteSqlRawAsync(insert, Guid.NewGuid(), userId, row.Food, row.Grams, row.Text);

            // Assert
            (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
        }
    }
}
