using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Models;
using Microsoft.EntityFrameworkCore;

namespace MarshallDisplayRegistry.Services;

public sealed class DeviceAuthService(DisplayRegistryContext db, TokenService tokenService)
{
    public async Task<DeviceAuthResult> AuthenticateAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var computerName = httpContext.Request.Headers["X-Display-ComputerName"].FirstOrDefault();
        var token = httpContext.Request.Headers["X-Display-Token"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(computerName) || string.IsNullOrWhiteSpace(token))
        {
            return DeviceAuthResult.Fail("Missing device authentication headers.");
        }

        var device = await db.DisplayDevices
            .Include(candidate => candidate.Credentials)
            .SingleOrDefaultAsync(candidate => candidate.ComputerName == computerName, cancellationToken);

        if (device is null)
        {
            return DeviceAuthResult.Fail("Unknown display device.");
        }

        var credential = device.Credentials
            .Where(candidate => candidate.RevokedUtc == null)
            .OrderByDescending(candidate => candidate.CreatedUtc)
            .FirstOrDefault(candidate => tokenService.VerifyToken(token, candidate.TokenHash));

        if (credential is null)
        {
            return DeviceAuthResult.Fail("Invalid display token.");
        }

        credential.LastUsedUtc = DateTime.UtcNow;
        return DeviceAuthResult.Success(device, credential);
    }
}

public sealed record DeviceAuthResult(bool IsAuthenticated, DisplayDevice? Device, DeviceCredential? Credential, string? Error)
{
    public static DeviceAuthResult Success(DisplayDevice device, DeviceCredential credential) => new(true, device, credential, null);
    public static DeviceAuthResult Fail(string error) => new(false, null, null, error);
}
