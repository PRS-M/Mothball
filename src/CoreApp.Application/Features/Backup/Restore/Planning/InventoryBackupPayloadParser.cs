using System.Text.Json;

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
        var createdUtcElement = GetRequiredProperty(document.RootElement, "createdUtc");
        RequireProperty(document.RootElement, "source");
        var integrity = GetRequiredProperty(document.RootElement, "integrity");
        var data = GetRequiredProperty(document.RootElement, "data");

        var createdUtc = createdUtcElement.GetDateTimeOffset();
        if (createdUtc == default)
        {
            throw new JsonException("Backup JSON payload createdUtc cannot be the default value.");
        }

        if (integrity.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Backup JSON payload integrity must be an object.");
        }

        RequireProperty(integrity, "checksumAlgorithm");
        RequireProperty(integrity, "payloadChecksum");

        if (data.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Backup JSON payload data must be an object.");
        }

        RequireProperty(data, "containers");
        RequireProperty(data, "items");
        RequireProperty(data, "relations");
        RequireProperty(data, "images");
    }

    private static void RequireProperty(JsonElement element, string propertyName)
    {
        _ = GetRequiredProperty(element, propertyName);
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string propertyName)
    {
        if (!element.EnumerateObject().Any(property =>
            string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new JsonException($"Backup JSON payload is missing required property '{propertyName}'.");
        }

        return element.EnumerateObject()
            .First(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            .Value;
    }
}
