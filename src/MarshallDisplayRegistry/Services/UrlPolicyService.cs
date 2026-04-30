using Microsoft.Extensions.Options;

namespace MarshallDisplayRegistry.Services;

public sealed class UrlPolicyService(IOptions<SignageOptions> options)
{
    private readonly SignageOptions _options = options.Value;

    public IReadOnlyList<string> AllowedDomains => _options.AllowedDomains;

    public bool IsAllowed(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return _options.AllowedDomains.Any(pattern => MatchesDomain(host, pattern));
    }

    private static bool MatchesDomain(string host, string pattern)
    {
        pattern = pattern.Trim().TrimEnd('.').ToLowerInvariant();
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            var suffix = pattern[2..];
            return host == suffix || host.EndsWith("." + suffix, StringComparison.Ordinal);
        }

        return host == pattern;
    }
}
