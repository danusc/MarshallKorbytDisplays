using System.ComponentModel.DataAnnotations;
using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Models;
using MarshallDisplayRegistry.Security;
using MarshallDisplayRegistry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarshallDisplayRegistry.Pages.Displays;

public sealed class EditModel(DisplayRegistryContext db, AuditService audit, AdminAuthService adminAuth) : PageModel
{
    [BindProperty]
    public DisplayInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            Input.Enabled = true;
            return Page();
        }

        var device = await db.DisplayDevices.FindAsync(id.Value);
        if (device is null)
        {
            return NotFound();
        }

        Input = DisplayInput.FromDevice(device);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTime.UtcNow;
        DisplayDevice device;
        object? oldValue = null;
        var action = "DisplayCreated";

        if (Input.Id.HasValue)
        {
            device = await db.DisplayDevices.FindAsync(Input.Id.Value) ?? throw new InvalidOperationException("Display not found.");
            oldValue = new { device.ComputerName, device.FriendlyName, device.Location, device.Building, device.Room, device.Enabled };
            action = "DisplayUpdated";
        }
        else
        {
            device = new DisplayDevice { CreatedUtc = now };
            db.DisplayDevices.Add(device);
        }

        device.ComputerName = Input.ComputerName.Trim().ToUpperInvariant();
        device.FriendlyName = Input.FriendlyName;
        device.Location = Input.Location;
        device.Building = Input.Building;
        device.Room = Input.Room;
        device.SerialNumber = Input.SerialNumber;
        device.MacAddress = Input.MacAddress;
        device.Enabled = Input.Enabled;
        device.UpdatedUtc = now;

        await db.SaveChangesAsync();
        audit.Add(adminAuth.GetActor(HttpContext), action, "DisplayDevice", device.Id, oldValue, Input);
        await db.SaveChangesAsync();
        return RedirectToPage("Details", new { id = device.Id });
    }

    public sealed class DisplayInput
    {
        public int? Id { get; set; }

        [Required, StringLength(128)]
        [Display(Name = "Computer name")]
        public string ComputerName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Friendly name")]
        public string? FriendlyName { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? Building { get; set; }

        [StringLength(100)]
        public string? Room { get; set; }

        [StringLength(200)]
        [Display(Name = "Serial number")]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "MAC address")]
        public string? MacAddress { get; set; }

        public bool Enabled { get; set; }

        public static DisplayInput FromDevice(DisplayDevice device) => new()
        {
            Id = device.Id,
            ComputerName = device.ComputerName,
            FriendlyName = device.FriendlyName,
            Location = device.Location,
            Building = device.Building,
            Room = device.Room,
            SerialNumber = device.SerialNumber,
            MacAddress = device.MacAddress,
            Enabled = device.Enabled
        };
    }
}
