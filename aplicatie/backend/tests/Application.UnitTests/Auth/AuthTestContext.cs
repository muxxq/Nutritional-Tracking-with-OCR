using Microsoft.EntityFrameworkCore;
using Moq;
using NutriScan.Application.Auth;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;
using NutriScan.Domain.Entities;
using NutriScan.Infrastructure.Persistence;

namespace NutriScan.Application.UnitTests.Auth;

/// <summary>
/// Shared arrangement for auth handler tests: an isolated in-memory database,
/// deterministic fakes for hashing and tokens, and a controllable clock.
/// </summary>
public sealed class AuthTestContext : IDisposable
{
    private int _refreshTokenCounter;

    public AuthTestContext()
    {
        Db = CreateContext();

        Hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => "hashed:" + p);
        Hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string hash, string p) => hash == "hashed:" + p ? PasswordCheckResult.Success : PasswordCheckResult.Failed);
        Hasher.Setup(h => h.DummyHash).Returns("hashed:dummy");

        Tokens.Setup(t => t.CreateAccessToken(It.IsAny<User>()))
            .Returns(() => new AccessToken("access", Now.AddMinutes(15)));
        Tokens.Setup(t => t.GenerateRefreshToken()).Returns(() => "rt-" + ++_refreshTokenCounter);
        Tokens.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns((string t) => "h:" + t);
        Tokens.Setup(t => t.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));

        Clock.Setup(c => c.UtcNow).Returns(() => Now);

        Issuer = new AuthSessionIssuer(Db, Tokens.Object, Clock.Object);
    }

    private readonly string _databaseName = $"auth-tests-{Guid.NewGuid()}";

    /// <summary>A separate unit of work on the same database, to simulate a concurrent request.</summary>
    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(_databaseName).Options, Clock.Object);

    public ApplicationDbContext Db { get; }
    public Mock<IPasswordHasher> Hasher { get; } = new();
    public Mock<ITokenService> Tokens { get; } = new();
    public Mock<IDateTimeProvider> Clock { get; } = new();
    public AuthSessionIssuer Issuer { get; }

    public DateTime Now { get; set; } = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    public async Task<User> SeedUserAsync(string email = "ana@exemplu.ro", string password = "parola123")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "hashed:" + password,
            FirstName = "Ana",
            LastName = "Pop"
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

    public async Task<RefreshToken> SeedRefreshTokenAsync(User user, string token, Guid familyId, bool revoked = false, DateTime? createdAtUtc = null)
    {
        var entity = RefreshToken.Create(user.Id, "h:" + token, familyId, createdAtUtc ?? Now, TimeSpan.FromDays(7));
        if (revoked)
        {
            entity.Revoke(Now);
        }
        Db.RefreshTokens.Add(entity);
        await Db.SaveChangesAsync();
        return entity;
    }

    public void Dispose() => Db.Dispose();
}
