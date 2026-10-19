using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using NutriScan.Infrastructure.Persistence;
using NutriScan.WebApi.IntegrationTests.Auth;

namespace NutriScan.WebApi.IntegrationTests.Foods;

[Collection(IntegrationTestCollection.Name)]
public class FoodsTests(NutriScanApiFactory factory) : IAsyncLifetime
{
    private static object Oats(string name = "Fulgi de ovăz bio", double calories = 389) => new
    {
        name, brand = "Ferma Mea", servingSizeGrams = 40, calories, proteins = 16.9, carbohydrates = 66.3,
        sugars = 1.0, fats = 6.9, saturatedFats = 1.2, fiber = 10.6, saltGrams = 0.01
    };

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

    private static async Task<string[]> NamesAsync(HttpResponseMessage response) =>
        (await JsonAsync(response)).GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToArray();

    private static async Task<string> CreateAsync(HttpClient client, object body) =>
        (await JsonAsync(await client.PostAsJsonAsync("/api/foods", body))).GetProperty("id").GetString()!;

    [Fact]
    public async Task Search_WithoutDiacritics_FindsSeededFood()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.GetAsync("/api/foods?query=piept%20pui%20fara%20piele");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await NamesAsync(response)).Should().ContainSingle(n => n.StartsWith("Piept de pui fără piele"));
    }

    [Fact]
    public async Task Food_CategoryRoundTrip()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var created = await JsonAsync(await client.PostAsJsonAsync("/api/foods", Oats()));
        var id = created.GetProperty("id").GetString();
        var updated = await JsonAsync(await client.PutAsJsonAsync($"/api/foods/{id}", new
        {
            name = "Lapte", calories = 46, proteins = 3.4, carbohydrates = 4.8, fats = 1.5, category = "dairy"
        }));

        // Assert
        created.GetProperty("category").GetString().Should().Be("other");
        updated.GetProperty("category").GetString().Should().Be("dairy");
    }

    [Fact]
    public async Task SeedFoods_HaveCategories()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var search = await JsonAsync(await client.GetAsync("/api/foods?query=piept%20pui%20fara%20piele"));
        var id = search.GetProperty("items")[0].GetProperty("id").GetString();

        // Act
        var food = await JsonAsync(await client.GetAsync($"/api/foods/{id}"));

        // Assert
        food.GetProperty("category").GetString().Should().Be("meatFish");
    }

    [Fact]
    public async Task Create_ThenSearch_OnlyAuthorSeesIt()
    {
        // Arrange
        var ana = await SignedInClientAsync("ana@exemplu.ro");
        var ion = await SignedInClientAsync("ion@exemplu.ro");

        // Act
        var create = await ana.PostAsJsonAsync("/api/foods", Oats());

        // Assert
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        create.Headers.Location!.ToString().Should().StartWith("/api/foods/");
        (await JsonAsync(create)).GetProperty("isOwn").GetBoolean().Should().BeTrue();
        (await NamesAsync(await ana.GetAsync("/api/foods?query=ovaz%20bio"))).Should().Equal("Fulgi de ovăz bio");
        (await NamesAsync(await ion.GetAsync("/api/foods?query=ovaz%20bio"))).Should().BeEmpty();
        (await NamesAsync(await ana.GetAsync("/api/foods?scope=mine"))).Should().Equal("Fulgi de ovăz bio");
    }

    [Fact]
    public async Task Create_InconsistentEnergy_Returns201WithWarning()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var body = new { name = "Batoane", calories = 250, proteins = 10, carbohydrates = 30, fats = 5 };

        // Act
        var response = await client.PostAsJsonAsync("/api/foods", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await JsonAsync(response)).GetProperty("energyWarning").GetString().Should().Contain("18%");
    }

    public static TheoryData<object> InvalidBodies => new()
    {
        new { name = "X", calories = 901, proteins = 0, carbohydrates = 0, fats = 0 },
        new { name = "X", calories = 400, proteins = 60, carbohydrates = 50, fats = 0 },
        new { name = "X", calories = 400, proteins = 0, carbohydrates = 10, sugars = 11, fats = 0 }
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Create_InvalidValues_Returns400(object body)
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/foods", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Create_MaxLengthNameAndBrand_Returns201()
    {
        // Arrange: SearchText = name + " " + brand = 250 + 1 + 150 characters
        var client = await SignedInClientAsync();
        var body = new { name = new string('a', 250), brand = new string('b', 150), calories = 100, proteins = 1, carbohydrates = 20, fats = 1 };

        // Act
        var response = await client.PostAsJsonAsync("/api/foods", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DeletingUser_WithPrivateFoods_IsRefusedSoFoodsNeverBecomeGlobal()
    {
        // Arrange: a private food; "creator = null" means global, so the FK must not be SET NULL
        var ana = await SignedInClientAsync("ana@exemplu.ro");
        var foodId = Guid.Parse(await CreateAsync(ana, Oats()));

        // Act
        Func<Task> deleteUser = async () =>
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Remove(await db.Users.SingleAsync());
            await db.SaveChangesAsync();
        };

        // Assert
        await deleteUser.Should().ThrowAsync<DbUpdateException>();
        using var verify = factory.Services.CreateScope();
        var food = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().FoodItems.SingleAsync(f => f.Id == foodId);
        food.CreatedByUserId.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_OwnFood_Returns200()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client, Oats());

        // Act
        var response = await client.PutAsJsonAsync($"/api/foods/{id}", Oats("Ovăz fin"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(response)).GetProperty("name").GetString().Should().Be("Ovăz fin");
        (await NamesAsync(await client.GetAsync("/api/foods?query=ovaz%20fin"))).Should().Equal("Ovăz fin");
    }

    [Fact]
    public async Task Archive_OwnFood_Returns204ThenGet404()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client, Oats());

        // Act
        var archive = await client.DeleteAsync($"/api/foods/{id}");

        // Assert
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/foods/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await NamesAsync(await client.GetAsync("/api/foods?scope=mine"))).Should().BeEmpty();
        (await client.DeleteAsync($"/api/foods/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Archive_FoodReferencedByDiary_KeepsDiaryRowReadable()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var foodId = Guid.Parse(await CreateAsync(client, Oats()));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync();
            db.DiaryEntries.Add(new DiaryEntry
            {
                UserId = user.Id, FoodItemId = foodId, QuantityGrams = 40,
                MealType = MealType.Breakfast, ConsumedDate = new DateOnly(2026, 10, 16)
            });
            await db.SaveChangesAsync();
        }

        // Act
        var archive = await client.DeleteAsync($"/api/foods/{foodId}");

        // Assert
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var verify = factory.Services.CreateScope();
        var entry = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .DiaryEntries.Include(d => d.FoodItem).SingleAsync();
        entry.FoodItem.Name.Should().Be("Fulgi de ovăz bio");
        entry.FoodItem.ArchivedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task OtherUsersFood_Returns404ForGetPutDelete()
    {
        // Arrange
        var ana = await SignedInClientAsync("ana@exemplu.ro");
        var ion = await SignedInClientAsync("ion@exemplu.ro");
        var id = await CreateAsync(ana, Oats());

        // Act & Assert
        (await ion.GetAsync($"/api/foods/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ion.PutAsJsonAsync($"/api/foods/{id}", Oats())).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ion.DeleteAsync($"/api/foods/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GlobalFood_Returns403ForPutDelete()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var search = await JsonAsync(await client.GetAsync("/api/foods?query=piept%20de%20pui"));
        var id = search.GetProperty("items")[0].GetProperty("id").GetString();

        // Act & Assert
        (await client.GetAsync($"/api/foods/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"/api/foods/{id}", Oats())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.DeleteAsync($"/api/foods/{id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SeededChicken_HasSaltConvertedFromSodium()
    {
        // Arrange
        var client = await SignedInClientAsync();
        var search = await JsonAsync(await client.GetAsync("/api/foods?query=piept%20de%20pui"));
        var id = search.GetProperty("items")[0].GetProperty("id").GetString();

        // Act
        var food = await JsonAsync(await client.GetAsync($"/api/foods/{id}"));

        // Assert: 65 mg sodium × 2.5 = 0.1625 g salt
        food.GetProperty("per100g").GetProperty("saltGrams").GetDouble().Should().Be(0.1625);
        food.GetProperty("isVerified").GetBoolean().Should().BeTrue();
        food.GetProperty("isOwn").GetBoolean().Should().BeFalse();
    }

    [Theory]
    [InlineData("%25", "%")]
    [InlineData("_", "_")]
    public async Task Search_WildcardCharacters_AreLiteral(string encodedQuery, string character)
    {
        // Arrange: as SQL wildcards, "%" and "_" would match every food in the seed
        var client = await SignedInClientAsync();

        // Act
        var response = await client.GetAsync($"/api/foods?query={encodedQuery}");

        // Assert: only foods literally containing the character ("Iaurt grecesc 2% grăsime" for "%")
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await NamesAsync(response)).Should().OnlyContain(name => name.Contains(character));
    }

    [Fact]
    public async Task Search_InvalidPaging_Returns400()
    {
        // Arrange
        var client = await SignedInClientAsync();

        // Act
        var response = await client.GetAsync("/api/foods?pageSize=51");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_HugePage_Returns400NotOverflow()
    {
        // Arrange: (page − 1) × pageSize would overflow int into a negative OFFSET
        var client = await SignedInClientAsync();

        // Act
        var response = await client.GetAsync("/api/foods?page=2147483647&pageSize=50");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("GET", "/api/foods")]
    [InlineData("POST", "/api/foods")]
    [InlineData("GET", "/api/foods/00000000-0000-0000-0000-000000000001")]
    public async Task Foods_WithoutToken_Returns401(string method, string url)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "POST" ? JsonContent.Create(Oats()) : null
        };

        // Act
        var response = await factory.CreateApiClient().SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
