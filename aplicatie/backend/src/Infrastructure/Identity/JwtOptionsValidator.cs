using System.Text;
using Microsoft.Extensions.Options;

namespace NutriScan.Infrastructure.Identity;

/// <summary>
/// Fails application startup when the JWT configuration is unsafe (HS256 needs a key of at least 256 bits).
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public const int MinimumSecretBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (Encoding.UTF8.GetByteCount(options.Secret ?? string.Empty) < MinimumSecretBytes)
        {
            failures.Add($"Jwt:Secret must be at least {MinimumSecretBytes} bytes. Set the Jwt__Secret environment variable.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
