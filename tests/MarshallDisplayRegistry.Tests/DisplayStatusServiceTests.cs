using MarshallDisplayRegistry.Models;
using MarshallDisplayRegistry.Services;

namespace MarshallDisplayRegistry.Tests;

public sealed class DisplayStatusServiceTests
{
    private readonly DisplayStatusService _service = new();
    private readonly DateTime _now = new(2026, 4, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetHealth_HealthyUntilThirtyMinuteOfflineThreshold()
    {
        var device = new DisplayDevice
        {
            Enabled = true,
            ChromeRunning = true,
            DesiredUrl = "https://marshall.usc.edu",
            CurrentUrl = "https://marshall.usc.edu",
            LastSeenUtc = _now.AddMinutes(-20)
        };

        Assert.Equal(DisplayHealth.Healthy, _service.GetHealth(device, nowUtc: _now));
    }

    [Fact]
    public void GetHealth_ReturnsPendingWhenAssignmentIsNewerThanLastSeen()
    {
        var device = new DisplayDevice
        {
            Enabled = true,
            ChromeRunning = true,
            DesiredUrl = "https://marshall.usc.edu",
            CurrentUrl = "https://old.marshall.usc.edu",
            LastSeenUtc = _now.AddMinutes(-5)
        };

        var assignment = new DisplayAssignment { CreatedUtc = _now.AddMinutes(-1) };

        Assert.Equal(DisplayHealth.Pending, _service.GetHealth(device, assignment, _now));
    }

    [Fact]
    public void GetHealth_ReturnsOfflineAfterThirtyMinutes()
    {
        var device = new DisplayDevice
        {
            Enabled = true,
            ChromeRunning = true,
            DesiredUrl = "https://marshall.usc.edu",
            CurrentUrl = "https://marshall.usc.edu",
            LastSeenUtc = _now.AddMinutes(-31)
        };

        Assert.Equal(DisplayHealth.Offline, _service.GetHealth(device, nowUtc: _now));
    }
}
