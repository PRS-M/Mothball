namespace CoreApp.Abstractions.Persistence;

/// <summary>
/// Maintenance operations for the active inventory data store.
/// Implementations may provide rollback and recovery capabilities.
/// </summary>
public interface IInventoryMaintenanceService
{
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
