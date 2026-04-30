using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace MarshallDisplayRegistry.Services;

public sealed class TokenService(IConfiguration configuration, IWebHostEnvironment environment)
{
    public string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public string HashToken(string token)
    {
        var secret = GetSecret();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
        return WebEncoders.Base64UrlEncode(hash);
    }

    public bool VerifyToken(string token, string expectedHash)
    {
        var actualHash = HashToken(token);
        var actualBytes = Encoding.UTF8.GetBytes(actualHash);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedHash);
        return actualBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    private string GetSecret()
    {
        var secret = configuration["Security:TokenHashSecret"] ?? configuration["TokenHashSecret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            return secret;
        }

        if (environment.IsDevelopment() || environment.EnvironmentName == "Testing")
        {
            return "local-development-token-hash-secret-change-before-production";
        }

        throw new InvalidOperationException("Security:TokenHashSecret is required outside development.");
    }
}
