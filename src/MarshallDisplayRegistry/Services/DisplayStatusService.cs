using MarshallDisplayRegistry.Models;

namespace MarshallDisplayRegistry.Services;

public sealed class DisplayStatusService
{
    public DisplayHealth GetHealth(DisplayDevice device, DisplayAssignment? latestAssignment = null, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        if (!device.Enabled)
        {
            return DisplayHealth.Disabled;
        }

        if (!string.IsNullOrWhiteSpace(device.LastError))
        {
            return DisplayHealth.Error;
        }

        if (!device.LastSeenUtc.HasValue || now - device.LastSeenUtc.Value > TimeSpan.FromMinutes(30))
        {
            return DisplayHealth.Offline;
        }

        if (latestAssignment is not null && latestAssignment.CreatedUtc > device.LastSeenUtc.Value)
        {
            return DisplayHealth.Pending;
        }

        if (string.IsNullOrWhiteSpace(device.DesiredUrl)
            || !device.ChromeRunning
            || !string.Equals(Normalize(device.CurrentUrl), Normalize(device.DesiredUrl), StringComparison.OrdinalIgnoreCase))
        {
            return DisplayHealth.NeedsRefresh;
        }

        return DisplayHealth.Healthy;
    }

    public string GetCssClass(DisplayHealth health) => health switch
    {
        DisplayHealth.Healthy => "status-healthy",
        DisplayHealth.Pending => "status-pending",
        DisplayHealth.NeedsRefresh => "status-refresh",
        DisplayHealth.Offline => "status-offline",
        DisplayHealth.Disabled => "status-disabled",
        DisplayHealth.Error => "status-error",
        _ => "status-disabled"
    };

    private static string? Normalize(string? url) => string.IsNullOrWhiteSpace(url) ? null : url.Trim().TrimEnd('/');
}
