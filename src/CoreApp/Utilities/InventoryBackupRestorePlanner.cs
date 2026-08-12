using CoreApp.Contracts;

namespace CoreApp.Utilities;

public static class InventoryBackupRestorePlanner
{
    private static readonly InventoryBackupRestorePlanBuilder PlanBuilder = new();

    public static InventoryBackupEnvelope ParseBackupJson(string backupJson)
    {
        return InventoryBackupPayloadParser.ParseBackupJson(backupJson);
    }

    public static void ValidatePayloadVersion(InventoryBackupEnvelope backup)
    {
        InventoryBackupPayloadVersionValidator.ValidatePayloadVersion(backup);
    }

    public static InventoryBackupEnvelope AttachIntegrity(
        InventoryBackupEnvelope backup,
        string? signatureSecret = null,
        string? keyId = null)
    {
        return InventoryBackupPayloadIntegrity.AttachIntegrity(backup, signatureSecret, keyId);
    }

    public static void ValidateIntegrity(
        InventoryBackupEnvelope backup,
        InventoryBackupRestoreOptions options)
    {
        InventoryBackupPayloadIntegrity.ValidateIntegrity(backup, options);
    }

    public static InventoryBackupRestorePlan BuildPlan(
        InventoryBackupEnvelope backup,
        InventoryBackupExistingState existingState,
        InventoryBackupConflictPolicy conflictPolicy)
    {
        return PlanBuilder.BuildPlan(backup, existingState, conflictPolicy);
    }
}
