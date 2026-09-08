namespace CoreApp.Application.Abstractions.Persistence;

/// <summary>
/// Maintenance operations for the active inventory data store.
/// Implementations may provide rollback and recovery capabilities.
/// </summary>
public interface IInventoryMaintenanceService
{
    /// <summary>
    /// Replaces all inventory-owned photos with the shared generic assets.
    /// </summary>
    /// <param name="progress">Optional progress callback for the long-running operation.</param>
    Task ReplaceAllPhotosWithSharedAssetsAsync(IProgress<MaintenanceProgress>? progress = null);

    /// <summary>
    /// Deletes all inventory data and recreates an empty operational store.
    /// </summary>
    /// <param name="progress">Optional progress callback for the long-running operation.</param>
    Task ResetAllDataAsync(IProgress<MaintenanceProgress>? progress = null);

    /// <summary>
    /// Attempts to recover the store to a usable state (best-effort).
    /// Intended to run at app startup.
    /// </summary>
    Task<bool> TryRecoverAsync();

    /// <summary>
    /// Attempts to rollback the most recent committed operation.
    /// </summary>
    Task<bool> TryRollbackLastCommitAsync();
}

/// <summary>
/// Reports progress for an inventory maintenance operation.
/// </summary>
/// <param name="Progress">The operation progress from zero to one.</param>
/// <param name="Status">A user-facing operation status.</param>
/// <param name="StepProgress">The current-step progress from zero to one.</param>
public readonly record struct MaintenanceProgress(
    double Progress,
    string Status,
    double StepProgress = 0);
