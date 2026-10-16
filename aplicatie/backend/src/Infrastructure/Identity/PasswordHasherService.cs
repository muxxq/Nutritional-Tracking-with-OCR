using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NutriScan.Application.Common.Models;
using NutriScan.Domain.Entities;
using IPasswordHasher = NutriScan.Application.Common.Interfaces.IPasswordHasher;

namespace NutriScan.Infrastructure.Identity;

/// <summary>
/// PBKDF2-HMAC-SHA512 with a per-user salt, via ASP.NET Core Identity's hasher.
/// The iteration count follows the OWASP 2023 recommendation.
/// </summary>
public sealed class PasswordHasherService : IPasswordHasher
{
    public const int IterationCount = 210_000;

    private readonly PasswordHasher<User> _hasher =
        new(Options.Create(new PasswordHasherOptions { IterationCount = IterationCount }));

    private readonly Lazy<string> _dummyHash;

    public PasswordHasherService()
    {
        _dummyHash = new Lazy<string>(() => Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
    }

    public string DummyHash => _dummyHash.Value;

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public PasswordCheckResult Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };
}
