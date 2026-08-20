using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Backup.Restore.Planning;

internal static class InventoryBackupPayloadVersionValidator
{
    public static void ValidatePayloadVersion(InventoryBackupEnvelope backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        if (backup.PayloadVersion != InventoryBackupEnvelope.CurrentPayloadVersion)
        {
            throw new NotSupportedException(
                $"Unsupported backup payload version '{backup.PayloadVersion}'. Expected '{InventoryBackupEnvelope.CurrentPayloadVersion}'.");
        }
    }
}
