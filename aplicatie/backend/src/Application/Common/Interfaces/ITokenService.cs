using NutriScan.Application.Common.Models;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Common.Interfaces;

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);

    TimeSpan RefreshTokenLifetime { get; }
}
