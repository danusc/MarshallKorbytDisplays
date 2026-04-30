using MarshallDisplayRegistry.Models;
using Microsoft.EntityFrameworkCore;

namespace MarshallDisplayRegistry.Data;

public sealed class SeedData(DisplayRegistryContext db)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (!await db.UrlProfiles.AnyAsync(cancellationToken))
        {
            db.UrlProfiles.AddRange(
                new UrlProfile
                {
                    Name = "Korbyt USC",
                    Url = "https://usc.korbyt.com",
                    Description = "Default Korbyt tenant landing page.",
                    Enabled = true,
                    CreatedUtc = now,
                    UpdatedUtc = now
                },
                new UrlProfile
                {
                    Name = "Marshall Home",
                    Url = "https://www.marshall.usc.edu",
                    Description = "Marshall School of Business website.",
                    Enabled = true,
                    CreatedUtc = now,
                    UpdatedUtc = now
                });
        }

        if (!await db.DisplayDevices.AnyAsync(device => device.ComputerName == "JFFVW-DSP-01", cancellationToken))
        {
            db.DisplayDevices.Add(new DisplayDevice
            {
                ComputerName = "JFFVW-DSP-01",
                FriendlyName = "Pilot Display",
                Location = "Pilot",
                Enabled = true,
                CreatedUtc = now,
                UpdatedUtc = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
