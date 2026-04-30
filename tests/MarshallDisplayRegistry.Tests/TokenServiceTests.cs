using MarshallDisplayRegistry.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace MarshallDisplayRegistry.Tests;

public sealed class TokenServiceTests
{
    [Fact]
    public void VerifyToken_AcceptsMatchingTokenAndRejectsDifferentToken()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:TokenHashSecret"] = "test-secret"
            })
            .Build();
        var service = new TokenService(configuration, new FakeEnvironment());
        var token = service.GenerateToken();
        var hash = service.HashToken(token);

        Assert.True(service.VerifyToken(token, hash));
        Assert.False(service.VerifyToken(token + "x", hash));
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
