using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NutriScan.WebApi.IntegrationTests.Auth;

/// <summary>Small helpers for calling the auth endpoints from integration tests.</summary>
public static class AuthApi
{
    public const string Password = "parola-sigura-123";
    public const string CookieName = "nutriscan_rt";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email = "ana@exemplu.ro") =>
        client.PostAsJsonAsync("/api/auth/register", new { email, password = Password, firstName = "Ana", lastName = "Pop" });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email = "ana@exemplu.ro", string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<string> ReadAccessTokenAsync(this HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    public static string? RefreshCookieHeader(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(CookieName + "=", StringComparison.Ordinal))
            : null;

    public static string? RefreshCookieValue(this HttpResponseMessage response)
    {
        var header = response.RefreshCookieHeader();
        return header?[(CookieName.Length + 1)..].Split(';')[0];
    }

    public static void UseBearer(this HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}
