namespace NutriScan.WebApi.Contracts.Auth;

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);
