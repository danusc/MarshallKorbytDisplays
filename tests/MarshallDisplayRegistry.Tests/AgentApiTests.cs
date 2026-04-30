using System.Net;
using System.Net.Http.Json;
using MarshallDisplayRegistry.Controllers;
using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MarshallDisplayRegistry.Tests;

public sealed class AgentApiTests : IClassFixture<AgentApiFactory>
{
    private readonly AgentApiFactory _factory;

    public AgentApiTests(AgentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_IssuesTokenOnceForDevice()
    {
        var client = _factory.CreateClient();
        var request = new RegisterRequest("DSP-01", "SERIAL", "AA-BB", "1.0.0");

        var first = await client.PostAsJsonAsync("/api/agent/register", request);
        var firstBody = await first.Content.ReadFromJsonAsync<RegisterResponse>();

        var second = await client.PostAsJsonAsync("/api/agent/register", request);
        var secondBody = await second.Content.ReadFromJsonAsync<RegisterResponse>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.False(firstBody!.Enabled);
        Assert.False(string.IsNullOrWhiteSpace(firstBody.Token));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Null(secondBody!.Token);
    }

    [Fact]
    public async Task CheckIn_ReturnsConfigAndPicksUpCommands()
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/agent/register", new RegisterRequest("DSP-02", null, null, "1.0.0"));
        var registered = await register.Content.ReadFromJsonAsync<RegisterResponse>();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DisplayRegistryContext>();
            var device = await db.DisplayDevices.SingleAsync(device => device.ComputerName == "DSP-02");
            device.Enabled = true;
            device.DesiredUrl = "https://marshall.usc.edu";
            db.DisplayCommands.Add(new DisplayCommand
            {
                DisplayDeviceId = device.Id,
                CommandType = DisplayCommandTypes.RestartChrome,
                Status = DisplayCommandStatuses.Queued,
                CreatedUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/agent/checkin")
        {
            Content = JsonContent.Create(new CheckInRequest(null, null, "1.0.0", "https://old.marshall.usc.edu", true, null))
        };
        request.Headers.Add("X-Display-ComputerName", "DSP-02");
        request.Headers.Add("X-Display-Token", registered!.Token);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<AgentConfigResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body!.Enabled);
        Assert.True(body.RestartChrome);
        Assert.Single(body.Commands);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DisplayRegistryContext>();
        var command = await verifyDb.DisplayCommands.SingleAsync();
        Assert.Equal(DisplayCommandStatuses.PickedUp, command.Status);
    }

    [Fact]
    public async Task Config_RejectsInvalidToken()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/agent/config");
        request.Headers.Add("X-Display-ComputerName", "DSP-404");
        request.Headers.Add("X-Display-Token", "bad-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class AgentApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:TokenHashSecret"] = "test-token-secret",
                ["SeedData:Enabled"] = "false",
                ["Signage:AutoEnableNewDevices"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DisplayRegistryContext>>();
            services.AddDbContext<DisplayRegistryContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
