namespace NutriScan.Application.Auth;

/// <summary>
/// User-facing auth error messages (Romanian, shown by the client).
/// </summary>
public static class AuthErrors
{
    public const string InvalidCredentials = "Email sau parolă incorectă";
    public const string InvalidSession = "Sesiune invalidă";
    public const string EmailTaken = "Există deja un cont cu acest email";
}
