using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutriScan.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NutriScan.WebApi.IntegrationTests;

/// <summary>
/// Hosts the real API in memory against a throwaway PostgreSQL container.
/// Requires a Docker-compatible engine (Docker, or rootless Podman via DOCKER_HOST).
/// </summary>
public class NutriScanApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string FrontendOrigin = "http://localhost:3000";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected virtual int AuthPermitLimit => 1000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Secret", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Cors:AllowedOrigins:0", FrontendOrigin);
        builder.UseSetting("RateLimiting:AuthPermitLimit", AuthPermitLimit.ToString());
    }

    public HttpClient CreateApiClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            // HTTPS so the cookie container sends the Secure refresh cookie back.
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.ShoppingListItems.ExecuteDeleteAsync();  // first: generated lines reference foods
        await db.PlannedMeals.ExecuteDeleteAsync();  // first: they reference diary entries, foods and recipes
        await db.DiaryEntries.ExecuteDeleteAsync();
        await db.Recipes.ExecuteDeleteAsync();  // ingredients cascade; they reference foods, deleted next
        await db.FoodItems.Where(f => f.CreatedByUserId != null).ExecuteDeleteAsync();  // keep the global seed
        await db.RefreshTokens.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // Building the host runs the startup migration against the container.
        _ = Services;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<NutriScanApiFactory>
{
    public const string Name = "api";
}
