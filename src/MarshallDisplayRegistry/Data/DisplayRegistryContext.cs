using MarshallDisplayRegistry.Models;
using Microsoft.EntityFrameworkCore;

namespace MarshallDisplayRegistry.Data;

public sealed class DisplayRegistryContext(DbContextOptions<DisplayRegistryContext> options) : DbContext(options)
{
    public DbSet<DisplayDevice> DisplayDevices => Set<DisplayDevice>();
    public DbSet<UrlProfile> UrlProfiles => Set<UrlProfile>();
    public DbSet<DisplayAssignment> DisplayAssignments => Set<DisplayAssignment>();
    public DbSet<DisplayCheckIn> DisplayCheckIns => Set<DisplayCheckIn>();
    public DbSet<DisplayCommand> DisplayCommands => Set<DisplayCommand>();
    public DbSet<DeviceCredential> DeviceCredentials => Set<DeviceCredential>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DisplayDevice>()
            .HasIndex(device => device.ComputerName)
            .IsUnique();

        modelBuilder.Entity<UrlProfile>()
            .HasIndex(profile => profile.Name)
            .IsUnique();

        modelBuilder.Entity<DeviceCredential>()
            .HasIndex(credential => credential.TokenHash);

        modelBuilder.Entity<DisplayAssignment>()
            .HasOne(assignment => assignment.DisplayDevice)
            .WithMany(device => device.Assignments)
            .HasForeignKey(assignment => assignment.DisplayDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DisplayAssignment>()
            .HasOne(assignment => assignment.UrlProfile)
            .WithMany(profile => profile.Assignments)
            .HasForeignKey(assignment => assignment.UrlProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DisplayCheckIn>()
            .HasOne(checkIn => checkIn.DisplayDevice)
            .WithMany(device => device.CheckIns)
            .HasForeignKey(checkIn => checkIn.DisplayDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DisplayCommand>()
            .HasOne(command => command.DisplayDevice)
            .WithMany(device => device.Commands)
            .HasForeignKey(command => command.DisplayDeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeviceCredential>()
            .HasOne(credential => credential.DisplayDevice)
            .WithMany(device => device.Credentials)
            .HasForeignKey(credential => credential.DisplayDeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
