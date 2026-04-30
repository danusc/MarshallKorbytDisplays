using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MarshallDisplayRegistry.Pages;

public sealed class IndexModel(DisplayRegistryContext db, DisplayStatusService statusService) : PageModel
{
    public IReadOnlyList<DashboardDisplay> Displays { get; private set; } = [];
    public IReadOnlyDictionary<string, int> StatusCounts { get; private set; } = new Dictionary<string, int>();

    public async Task OnGetAsync()
    {
        var devices = await db.DisplayDevices
            .Include(device => device.Assignments.OrderByDescending(assignment => assignment.CreatedUtc).Take(1))
            .OrderBy(device => device.ComputerName)
            .ToListAsync();

        Displays = devices.Select(device =>
        {
            var status = statusService.GetHealth(device, device.Assignments.FirstOrDefault());
            return new DashboardDisplay(
                device.Id,
                device.ComputerName,
                device.FriendlyName ?? string.Empty,
                status.ToString(),
                statusService.GetCssClass(status),
                device.DesiredUrl ?? string.Empty,
                device.CurrentUrl ?? string.Empty,
                device.LastSeenUtc?.ToLocalTime().ToString("g") ?? "Never");
        }).ToList();

        StatusCounts = Displays
            .GroupBy(display => display.Status)
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    public sealed record DashboardDisplay(int Id, string ComputerName, string FriendlyName, string Status, string StatusCss, string DesiredUrl, string CurrentUrl, string LastSeen);
}
