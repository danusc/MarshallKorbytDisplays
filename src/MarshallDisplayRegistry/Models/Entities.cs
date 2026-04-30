using System.ComponentModel.DataAnnotations;

namespace MarshallDisplayRegistry.Models;

public sealed class DisplayDevice
{
    public int Id { get; set; }

    [Required, StringLength(128)]
    public string ComputerName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? FriendlyName { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(100)]
    public string? Building { get; set; }

    [StringLength(100)]
    public string? Room { get; set; }

    [StringLength(200)]
    public string? SerialNumber { get; set; }

    [StringLength(100)]
    public string? MacAddress { get; set; }

    public bool Enabled { get; set; }

    [StringLength(50)]
    public string? AgentVersion { get; set; }

    public DateTime? LastSeenUtc { get; set; }

    [StringLength(2048)]
    public string? CurrentUrl { get; set; }

    [StringLength(2048)]
    public string? DesiredUrl { get; set; }

    public bool ChromeRunning { get; set; }

    [StringLength(2048)]
    public string? LastError { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<DisplayAssignment> Assignments { get; set; } = new List<DisplayAssignment>();
    public ICollection<DisplayCheckIn> CheckIns { get; set; } = new List<DisplayCheckIn>();
    public ICollection<DisplayCommand> Commands { get; set; } = new List<DisplayCommand>();
    public ICollection<DeviceCredential> Credentials { get; set; } = new List<DeviceCredential>();
}

public sealed class UrlProfile
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<DisplayAssignment> Assignments { get; set; } = new List<DisplayAssignment>();
}

public sealed class DisplayAssignment
{
    public int Id { get; set; }
    public int DisplayDeviceId { get; set; }
    public DisplayDevice? DisplayDevice { get; set; }
    public int UrlProfileId { get; set; }
    public UrlProfile? UrlProfile { get; set; }

    [StringLength(256)]
    public string? AssignedBy { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedUtc { get; set; }
}

public sealed class DisplayCheckIn
{
    public int Id { get; set; }
    public int DisplayDeviceId { get; set; }
    public DisplayDevice? DisplayDevice { get; set; }
    public DateTime CheckInUtc { get; set; }

    [StringLength(128)]
    public string ComputerName { get; set; } = string.Empty;

    [StringLength(2048)]
    public string? CurrentUrl { get; set; }

    [StringLength(2048)]
    public string? DesiredUrl { get; set; }

    public bool ChromeRunning { get; set; }

    [StringLength(50)]
    public string? AgentVersion { get; set; }

    [StringLength(64)]
    public string? IpAddress { get; set; }

    [StringLength(2048)]
    public string? ErrorMessage { get; set; }
}

public sealed class DisplayCommand
{
    public int Id { get; set; }
    public int DisplayDeviceId { get; set; }
    public DisplayDevice? DisplayDevice { get; set; }

    [Required, StringLength(80)]
    public string CommandType { get; set; } = DisplayCommandTypes.RestartChrome;

    [Required, StringLength(80)]
    public string Status { get; set; } = DisplayCommandStatuses.Queued;

    public DateTime CreatedUtc { get; set; }
    public DateTime? PickedUpUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }

    [StringLength(2048)]
    public string? ResultMessage { get; set; }
}

public sealed class DeviceCredential
{
    public int Id { get; set; }
    public int DisplayDeviceId { get; set; }
    public DisplayDevice? DisplayDevice { get; set; }

    [Required, StringLength(256)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
    public DateTime? LastUsedUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
}

public sealed class AuditLog
{
    public int Id { get; set; }

    [Required, StringLength(256)]
    public string Actor { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ObjectType { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ObjectId { get; set; } = string.Empty;

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public static class DisplayCommandTypes
{
    public const string RestartChrome = "RestartChrome";
}

public static class DisplayCommandStatuses
{
    public const string Queued = "Queued";
    public const string PickedUp = "PickedUp";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public enum DisplayHealth
{
    Healthy,
    Pending,
    NeedsRefresh,
    Offline,
    Disabled,
    Error
}
