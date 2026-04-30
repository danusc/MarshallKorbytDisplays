using MarshallDisplayRegistry.Services;
using Microsoft.Extensions.Options;

namespace MarshallDisplayRegistry.Tests;

public sealed class UrlPolicyServiceTests
{
    [Theory]
    [InlineData("https://www.usc.edu")]
    [InlineData("https://marshall.usc.edu/news")]
    [InlineData("https://usc.korbyt.com/player")]
    public void IsAllowed_AcceptsConfiguredDomains(string url)
    {
        var service = new UrlPolicyService(Options.Create(new SignageOptions()));

        Assert.True(service.IsAllowed(url));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not-a-url")]
    public void IsAllowed_RejectsUntrustedUrls(string url)
    {
        var service = new UrlPolicyService(Options.Create(new SignageOptions()));

        Assert.False(service.IsAllowed(url));
    }
}
