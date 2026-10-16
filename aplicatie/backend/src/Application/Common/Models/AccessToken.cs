namespace NutriScan.Application.Common.Models;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
