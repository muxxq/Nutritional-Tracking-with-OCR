using NutriScan.Domain.Common;
using NutriScan.Domain.Exceptions;

namespace NutriScan.Domain.Entities;

/// <summary>
/// A long-lived credential used only to obtain new access tokens. Only the SHA-256 hash
/// of the token is stored. All rotations of one login share the same <see cref="FamilyId"/>,
/// which allows revoking the whole chain when a rotated token is reused.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string TokenHash { get; private set; } = string.Empty;
    public Guid FamilyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    // Required by EF Core
    private RefreshToken()
    {
    }

    public static RefreshToken Create(Guid userId, string tokenHash, Guid familyId, DateTime nowUtc, TimeSpan lifetime) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc + lifetime
        };

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    public void Revoke(DateTime nowUtc, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            throw new DomainException("Refresh token is already revoked.");
        }

        RevokedAtUtc = nowUtc;
        ReplacedByTokenId = replacedByTokenId;
    }
}
