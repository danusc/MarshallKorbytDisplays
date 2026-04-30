using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Models;
using MarshallDisplayRegistry.Security;
using MarshallDisplayRegistry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MarshallDisplayRegistry.Pages.UrlProfiles;

public sealed class IndexModel(DisplayRegistryContext db, AuditService audit, AdminAuthService adminAuth) : PageModel
{
    public IReadOnlyList<UrlProfile> Profiles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Profiles = await db.UrlProfiles.OrderBy(profile => profile.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var profile = await db.UrlProfiles.FindAsync(id);
        if (profile is null)
        {
            return NotFound();
        }

        var oldValue = profile.Enabled;
        profile.Enabled = !profile.Enabled;
        profile.UpdatedUtc = DateTime.UtcNow;
        audit.Add(adminAuth.GetActor(HttpContext), profile.Enabled ? "UrlProfileEnabled" : "UrlProfileDisabled", "UrlProfile", profile.Id, oldValue, profile.Enabled);
        await db.SaveChangesAsync();
        return RedirectToPage();
    }
}
