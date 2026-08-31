using System.Text.Json;
using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Backup.Restore.Planning;

internal static class InventoryBackupPayloadParser
{
    private static readonly JsonSerializerOptions BackupJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static InventoryBackupEnvelope ParseBackupJson(string backupJson)
    {
        if (string.IsNullOrWhiteSpace(backupJson))
        {
            throw new ArgumentException("Backup JSON cannot be null or empty.", nameof(backupJson));
        }

        InventoryBackupEnvelope? backup;
        try
        {
            ValidateRequiredTopLevelProperties(backupJson);
            backup = JsonSerializer.Deserialize<InventoryBackupEnvelope>(backupJson, BackupJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Backup JSON payload is invalid.", nameof(backupJson), ex);
        }

        if (backup is null)
        {
            throw new ArgumentException("Backup JSON payload is invalid.", nameof(backupJson));
        }

        return backup;
    }

    private static void ValidateRequiredTopLevelProperties(string backupJson)
    {
        using var document = JsonDocument.Parse(backupJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Backup JSON root must be an object.");
        }

        RequireProperty(document.RootElement, "payloadVersion");
        RequireProperty(document.RootElement, "schemaVersion");
        RequireProperty(document.RootElement, "createdUtc");
        RequireProperty(document.RootElement, "source");
        RequireProperty(document.RootElement, "integrity");
        RequireProperty(document.RootElement, "data");
    }

    private static void RequireProperty(JsonElement element, string propertyName)
    {
        if (!element.EnumerateObject().Any(property =>
            string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new JsonException($"Backup JSON payload is missing required property '{propertyName}'.");
        }
    }
}
