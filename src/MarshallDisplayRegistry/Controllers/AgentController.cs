using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Models;
using MarshallDisplayRegistry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarshallDisplayRegistry.Controllers;

[ApiController]
[Route("api/agent")]
public sealed class AgentController(
    DisplayRegistryContext db,
    DeviceAuthService deviceAuth,
    TokenService tokenService,
    UrlPolicyService urlPolicy,
    IOptions<SignageOptions> options,
    ILogger<AgentController> logger) : ControllerBase
{
    private readonly SignageOptions _options = options.Value;

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ComputerName))
        {
            return BadRequest("computerName is required.");
        }

        var now = DateTime.UtcNow;
        var computerName = request.ComputerName.Trim().ToUpperInvariant();
        var device = await db.DisplayDevices
            .Include(candidate => candidate.Credentials)
            .SingleOrDefaultAsync(candidate => candidate.ComputerName == computerName, cancellationToken);

        if (device is null)
        {
            device = new DisplayDevice
            {
                ComputerName = computerName,
                Enabled = _options.AutoEnableNewDevices,
                CreatedUtc = now,
                UpdatedUtc = now
            };
            db.DisplayDevices.Add(device);
        }

        device.SerialNumber = request.SerialNumber;
        device.MacAddress = request.MacAddress;
        device.AgentVersion = request.AgentVersion;
        device.UpdatedUtc = now;

        var activeTokenExists = device.Credentials.Any(credential => credential.RevokedUtc == null);
        string? token = null;
        if (!activeTokenExists)
        {
            token = tokenService.GenerateToken();
            device.Credentials.Add(new DeviceCredential
            {
                TokenHash = tokenService.HashToken(token),
                CreatedUtc = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Display device {ComputerName} registered. Token issued: {TokenIssued}", computerName, token is not null);
        return new RegisterResponse(device.Id, device.Enabled, device.DesiredUrl, _options.LaunchMode, _options.DefaultPollSeconds, urlPolicy.AllowedDomains, token);
    }

    [HttpPost("checkin")]
    public async Task<ActionResult<AgentConfigResponse>> CheckIn(CheckInRequest request, CancellationToken cancellationToken)
    {
        var auth = await deviceAuth.AuthenticateAsync(HttpContext, cancellationToken);
        if (!auth.IsAuthenticated || auth.Device is null)
        {
            return Unauthorized(auth.Error);
        }

        var now = DateTime.UtcNow;
        var device = auth.Device;
        device.SerialNumber = request.SerialNumber;
        device.MacAddress = request.MacAddress;
        device.AgentVersion = request.AgentVersion;
        device.CurrentUrl = request.CurrentUrl;
        device.ChromeRunning = request.ChromeRunning;
        device.LastError = request.LastError;
        device.LastSeenUtc = now;
        device.UpdatedUtc = now;

        db.DisplayCheckIns.Add(new DisplayCheckIn
        {
            DisplayDeviceId = device.Id,
            CheckInUtc = now,
            ComputerName = device.ComputerName,
            CurrentUrl = request.CurrentUrl,
            DesiredUrl = device.DesiredUrl,
            ChromeRunning = request.ChromeRunning,
            AgentVersion = request.AgentVersion,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ErrorMessage = request.LastError
        });

        var pendingCommands = await db.DisplayCommands
            .Where(command => command.DisplayDeviceId == device.Id && command.Status == DisplayCommandStatuses.Queued)
            .OrderBy(command => command.CreatedUtc)
            .ToListAsync(cancellationToken);

        foreach (var command in pendingCommands)
        {
            command.Status = DisplayCommandStatuses.PickedUp;
            command.PickedUpUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return BuildConfig(device, pendingCommands);
    }

    [HttpGet("config")]
    public async Task<ActionResult<AgentConfigResponse>> Config(CancellationToken cancellationToken)
    {
        var auth = await deviceAuth.AuthenticateAsync(HttpContext, cancellationToken);
        if (!auth.IsAuthenticated || auth.Device is null)
        {
            return Unauthorized(auth.Error);
        }

        await db.SaveChangesAsync(cancellationToken);
        return BuildConfig(auth.Device, []);
    }

    [HttpPost("commands/{id:int}/complete")]
    public async Task<IActionResult> CompleteCommand(int id, CompleteCommandRequest request, CancellationToken cancellationToken)
    {
        var auth = await deviceAuth.AuthenticateAsync(HttpContext, cancellationToken);
        if (!auth.IsAuthenticated || auth.Device is null)
        {
            return Unauthorized(auth.Error);
        }

        var command = await db.DisplayCommands
            .SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.DisplayDeviceId == auth.Device.Id, cancellationToken);

        if (command is null)
        {
            return NotFound();
        }

        command.Status = string.IsNullOrWhiteSpace(request.Status) ? DisplayCommandStatuses.Completed : request.Status;
        command.ResultMessage = request.ResultMessage;
        command.CompletedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private AgentConfigResponse BuildConfig(DisplayDevice device, IReadOnlyCollection<DisplayCommand> commands)
    {
        return new AgentConfigResponse(
            device.Enabled,
            device.Enabled ? device.DesiredUrl : null,
            _options.LaunchMode,
            commands.Any(command => command.CommandType == DisplayCommandTypes.RestartChrome),
            _options.DefaultPollSeconds,
            urlPolicy.AllowedDomains,
            commands.Select(command => new AgentCommandDto(command.Id, command.CommandType)).ToList());
    }
}

public sealed record RegisterRequest(string ComputerName, string? SerialNumber, string? MacAddress, string? AgentVersion);
public sealed record RegisterResponse(int DeviceId, bool Enabled, string? DesiredUrl, string LaunchMode, int PollSeconds, IReadOnlyList<string> AllowedDomains, string? Token);
public sealed record CheckInRequest(string? SerialNumber, string? MacAddress, string? AgentVersion, string? CurrentUrl, bool ChromeRunning, string? LastError);
public sealed record CompleteCommandRequest(string Status, string? ResultMessage);
public sealed record AgentConfigResponse(bool Enabled, string? DesiredUrl, string LaunchMode, bool RestartChrome, int PollSeconds, IReadOnlyList<string> AllowedDomains, IReadOnlyList<AgentCommandDto> Commands);
public sealed record AgentCommandDto(int Id, string CommandType);
