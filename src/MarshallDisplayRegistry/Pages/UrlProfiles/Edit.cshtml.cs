using System.ComponentModel.DataAnnotations;
using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Models;
using MarshallDisplayRegistry.Security;
using MarshallDisplayRegistry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarshallDisplayRegistry.Pages.UrlProfiles;

public sealed class EditModel(
    DisplayRegistryContext db,
    UrlPolicyService urlPolicy,
    AuditService audit,
    AdminAuthService adminAuth) : PageModel
{
    [BindProperty]
    public UrlProfileInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            Input.Enabled = true;
            return Page();
        }

        var profile = await db.UrlProfiles.FindAsync(id.Value);
        if (profile is null)
        {
            return NotFound();
        }

        Input = UrlProfileInput.FromProfile(profile);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!urlPolicy.IsAllowed(Input.Url))
        {
            ModelState.AddModelError("Input.Url", "URL must be HTTP/HTTPS and match an allowed domain.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTime.UtcNow;
        UrlProfile profile;
        object? oldValue = null;
        var action = "UrlProfileCreated";

        if (Input.Id.HasValue)
        {
            profile = await db.UrlProfiles.FindAsync(Input.Id.Value) ?? throw new InvalidOperationException("URL profile not found.");
            oldValue = new { profile.Name, profile.Url, profile.Description, profile.Enabled };
            action = "UrlProfileUpdated";
        }
        else
        {
            profile = new UrlProfile { CreatedUtc = now };
            db.UrlProfiles.Add(profile);
        }

        profile.Name = Input.Name.Trim();
        profile.Url = Input.Url.Trim();
        profile.Description = Input.Description;
        profile.Enabled = Input.Enabled;
        profile.UpdatedUtc = now;

        await db.SaveChangesAsync();
        audit.Add(adminAuth.GetActor(HttpContext), action, "UrlProfile", profile.Id, oldValue, Input);
        await db.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    public sealed class UrlProfileInput
    {
        public int? Id { get; set; }

        [Required, StringLength(160)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(2048), Url]
        public string Url { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool Enabled { get; set; }

        public static UrlProfileInput FromProfile(UrlProfile profile) => new()
        {
            Id = profile.Id,
            Name = profile.Name,
            Url = profile.Url,
            Description = profile.Description,
            Enabled = profile.Enabled
        };
    }
}
