using System.Text.Json;
using MarshallDisplayRegistry.Data;
using MarshallDisplayRegistry.Models;

namespace MarshallDisplayRegistry.Services;

public sealed class AuditService(DisplayRegistryContext db)
{
    public void Add(string actor, string action, string objectType, object objectId, object? oldValue = null, object? newValue = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Actor = string.IsNullOrWhiteSpace(actor) ? "unknown" : actor,
            Action = action,
            ObjectType = objectType,
            ObjectId = objectId.ToString() ?? string.Empty,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
            CreatedUtc = DateTime.UtcNow
        });
    }
}
