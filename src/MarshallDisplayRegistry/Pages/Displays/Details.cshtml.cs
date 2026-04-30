using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Models;
using MarshallDisplayRegistry.Security;
using MarshallDisplayRegistry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MarshallDisplayRegistry.Pages.Displays;

public sealed class DetailsModel(
    DisplayRegistryContext db,
    DisplayStatusService statusService,
    UrlPolicyService urlPolicy,
    AuditService audit,
    AdminAuthService adminAuth) : PageModel
{
    public DisplayDevice? Device { get; private set; }
    public IReadOnlyList<UrlProfile> UrlProfiles { get; private set; } = [];
    public IReadOnlyList<DisplayCheckIn> RecentCheckIns { get; private set; } = [];
    public IReadOnlyList<DisplayCommand> RecentCommands { get; private set; } = [];
    public string Status { get; private set; } = string.Empty;
    public string StatusCss { get; private set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadAsync(id);
    }

    public async Task<IActionResult> OnPostAssignAsync(int id, int urlProfileId, string? notes)
    {
        var device = await db.DisplayDevices.FindAsync(id);
        var profile = await db.UrlProfiles.FindAsync(urlProfileId);
        if (device is null || profile is null)
        {
            return NotFound();
        }

        if (!profile.Enabled || !urlPolicy.IsAllowed(profile.Url))
        {
            ModelState.AddModelError(string.Empty, "Selected URL profile is disabled or outside the allowed domains.");
            return await LoadAsync(id);
        }

        var oldUrl = device.DesiredUrl;
        var now = DateTime.UtcNow;
        device.DesiredUrl = profile.Url;
        device.UpdatedUtc = now;
        db.DisplayAssignments.Add(new DisplayAssignment
        {
            DisplayDeviceId = device.Id,
            UrlProfileId = profile.Id,
            AssignedBy = adminAuth.GetActor(HttpContext),
            Notes = notes,
            CreatedUtc = now
        });
        audit.Add(adminAuth.GetActor(HttpContext), "DisplayUrlAssigned", "DisplayDevice", device.Id, oldUrl, profile.Url);
        await db.SaveChangesAsync();
        Message = "URL profile assigned.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostQueueRestartAsync(int id)
    {
        var device = await db.DisplayDevices.FindAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        db.DisplayCommands.Add(new DisplayCommand
        {
            DisplayDeviceId = device.Id,
            CommandType = DisplayCommandTypes.RestartChrome,
            Status = DisplayCommandStatuses.Queued,
            CreatedUtc = DateTime.UtcNow
        });
        audit.Add(adminAuth.GetActor(HttpContext), "DisplayCommandQueued", "DisplayDevice", device.Id, null, DisplayCommandTypes.RestartChrome);
        await db.SaveChangesAsync();
        Message = "RestartChrome command queued.";
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult> LoadAsync(int id)
    {
        Device = await db.DisplayDevices
            .Include(device => device.Assignments.OrderByDescending(assignment => assignment.CreatedUtc).Take(1))
            .SingleOrDefaultAsync(device => device.Id == id);

        if (Device is null)
        {
            return NotFound();
        }

        var health = statusService.GetHealth(Device, Device.Assignments.FirstOrDefault());
        Status = health.ToString();
        StatusCss = statusService.GetCssClass(health);
        UrlProfiles = await db.UrlProfiles.Where(profile => profile.Enabled).OrderBy(profile => profile.Name).ToListAsync();
        RecentCheckIns = await db.DisplayCheckIns
            .Where(checkIn => checkIn.DisplayDeviceId == id)
            .OrderByDescending(checkIn => checkIn.CheckInUtc)
            .Take(10)
            .ToListAsync();
        RecentCommands = await db.DisplayCommands
            .Where(command => command.DisplayDeviceId == id)
            .OrderByDescending(command => command.CreatedUtc)
            .Take(10)
            .ToListAsync();

        return Page();
    }
}
