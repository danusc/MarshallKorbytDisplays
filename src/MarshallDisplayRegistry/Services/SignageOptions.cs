namespace MarshallDisplayRegistry.Services;

public sealed class SignageOptions
{
    public const string SectionName = "Signage";

    public string[] AllowedDomains { get; set; } = ["*.usc.edu", "usc.korbyt.com"];
    public int DefaultPollSeconds { get; set; } = 300;
    public string LaunchMode { get; set; } = "kiosk";
    public bool AutoEnableNewDevices { get; set; }
    public string AdminGroupObjectId { get; set; } = "f1a3237e-1402-4f30-82a7-bfdb0c70c1aa";
}
