using CoreApp.Contracts;
using CoreApp.Domain.Inventory;
using Microsoft.Extensions.Logging;

namespace CoreApp.Features.Backup.Restore.Planning;

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
        InventoryBackupRestoreOptions options,
        ILogger? logger = null)
    {
        InventoryBackupPayloadIntegrity.ValidateIntegrity(backup, options, logger);
    }

    public static InventoryBackupRestorePlan BuildPlan(
        InventoryBackupEnvelope backup,
        InventoryBackupExistingState existingState,
        InventoryBackupConflictPolicy conflictPolicy)
    {
        return PlanBuilder.BuildPlan(backup, existingState, conflictPolicy);
    }

    /// <summary>
    /// Builds a restore plan using a format-agnostic inventory merge policy.
    /// </summary>
    public static InventoryBackupRestorePlan BuildPlan(
        InventoryBackupEnvelope backup,
        InventoryBackupExistingState existingState,
        InventoryMergePolicy mergePolicy)
    {
        return PlanBuilder.BuildPlan(backup, existingState, mergePolicy);
    }
}
