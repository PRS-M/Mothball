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
}
