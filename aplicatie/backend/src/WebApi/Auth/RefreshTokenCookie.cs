namespace NutriScan.WebApi.Auth;

/// <summary>
/// The refresh token cookie: unreadable from JavaScript (HttpOnly), HTTPS only (Secure),
/// never sent cross-site (SameSite=Strict) and only sent to the auth endpoints (Path).
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "nutriscan_rt";
    private const string Path = "/api/auth";

    public static void Append(HttpResponse response, string token, TimeSpan lifetime) =>
        response.Cookies.Append(Name, token, CreateOptions(lifetime));

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, CreateOptions(maxAge: null));

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) ? value : null;

    private static CookieOptions CreateOptions(TimeSpan? maxAge) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        MaxAge = maxAge,
        IsEssential = true
    };
}
