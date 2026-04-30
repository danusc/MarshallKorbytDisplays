using System.Text;
using System.Text.Json;
using MarshallDisplayRegistry.Services;
using Microsoft.Extensions.Options;

namespace MarshallDisplayRegistry.Security;

public sealed class AdminAuthService(IConfiguration configuration, IWebHostEnvironment environment, IOptions<SignageOptions> options)
{
    public bool IsAdmin(HttpContext context)
    {
        if (environment.IsDevelopment() && configuration.GetValue("AdminAuth:BypassLocal", true))
        {
            return true;
        }

        var requiredGroupId = options.Value.AdminGroupObjectId;
        if (string.IsNullOrWhiteSpace(requiredGroupId))
        {
            return false;
        }

        return GetClaims(context).Any(claim =>
            IsGroupClaim(claim.Type) && string.Equals(claim.Value, requiredGroupId, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasEasyAuthPrincipal(HttpContext context) =>
        !string.IsNullOrWhiteSpace(context.Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault());

    public string GetActor(HttpContext context)
    {
        var name = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].FirstOrDefault()
            ?? GetClaims(context).FirstOrDefault(claim => IsNameClaim(claim.Type))?.Value
            ?? context.User.Identity?.Name;

        return string.IsNullOrWhiteSpace(name) ? "local-admin" : name;
    }

    private static IEnumerable<EasyAuthClaim> GetClaims(HttpContext context)
    {
        var encodedPrincipal = context.Request.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(encodedPrincipal))
        {
            yield break;
        }

        string json;
        try
        {
            json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
        }
        catch (FormatException)
        {
            yield break;
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("claims", out var claims))
        {
            yield break;
        }

        foreach (var claim in claims.EnumerateArray())
        {
            var type = claim.TryGetProperty("typ", out var typeElement) ? typeElement.GetString() : null;
            var value = claim.TryGetProperty("val", out var valueElement) ? valueElement.GetString() : null;
            if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(value))
            {
                yield return new EasyAuthClaim(type, value);
            }
        }
    }

    private static bool IsGroupClaim(string type) =>
        type.Equals("groups", StringComparison.OrdinalIgnoreCase)
        || type.Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/groups", StringComparison.OrdinalIgnoreCase);

    private static bool IsNameClaim(string type) =>
        type.Equals("name", StringComparison.OrdinalIgnoreCase)
        || type.Equals("preferred_username", StringComparison.OrdinalIgnoreCase)
        || type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", StringComparison.OrdinalIgnoreCase);

    private sealed record EasyAuthClaim(string Type, string Value);
}
