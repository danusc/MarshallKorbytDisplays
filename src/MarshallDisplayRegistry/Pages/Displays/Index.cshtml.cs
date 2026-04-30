using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Security;
using MarshallDisplayRegistry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MarshallDisplayRegistry.Pages.Displays;

public sealed class IndexModel(
    DisplayRegistryContext db,
    DisplayStatusService statusService,
    AuditService audit,
    AdminAuthService adminAuth) : PageModel
{
    public string? Search { get; private set; }
    public IReadOnlyList<DisplayRow> Displays { get; private set; } = [];

    public async Task OnGetAsync(string? search)
    {
        Search = search;
        var query = db.DisplayDevices
            .Include(device => device.Assignments.OrderByDescending(assignment => assignment.CreatedUtc).Take(1))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(device =>
                device.ComputerName.Contains(search)
                || (device.FriendlyName != null && device.FriendlyName.Contains(search))
                || (device.Location != null && device.Location.Contains(search))
                || (device.Building != null && device.Building.Contains(search))
                || (device.Room != null && device.Room.Contains(search)));
        }

        var devices = await query.OrderBy(device => device.ComputerName).ToListAsync();
        Displays = devices.Select(device =>
        {
            var health = statusService.GetHealth(device, device.Assignments.FirstOrDefault());
            return new DisplayRow(
                device.Id,
                device.ComputerName,
                device.FriendlyName ?? string.Empty,
                string.Join(" / ", new[] { device.Building, device.Room, device.Location }.Where(value => !string.IsNullOrWhiteSpace(value))),
                device.DesiredUrl ?? string.Empty,
                device.LastSeenUtc?.ToLocalTime().ToString("g") ?? "Never",
                device.Enabled,
                health.ToString(),
                statusService.GetCssClass(health));
        }).ToList();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var device = await db.DisplayDevices.FindAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        var oldValue = device.Enabled;
        device.Enabled = !device.Enabled;
        device.UpdatedUtc = DateTime.UtcNow;
        audit.Add(adminAuth.GetActor(HttpContext), device.Enabled ? "DisplayEnabled" : "DisplayDisabled", "DisplayDevice", device.Id, oldValue, device.Enabled);
        await db.SaveChangesAsync();
        return RedirectToPage(new { Search });
    }

    public sealed record DisplayRow(int Id, string ComputerName, string FriendlyName, string LocationSummary, string DesiredUrl, string LastSeen, bool Enabled, string Status, string StatusCss);
}
