using NutriScan.Application.Common.Models;

namespace NutriScan.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordCheckResult Verify(string passwordHash, string password);

    /// <summary>
    /// A valid hash of a random secret. Verifying against it when a user does not exist
    /// keeps login response times constant, so attackers cannot detect registered emails.
    /// </summary>
    string DummyHash { get; }
}
