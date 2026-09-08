namespace Infrastructure.Services.JsonStore;

public sealed class JsonInventoryMaintenanceService : IInventoryMaintenanceService
{
    private readonly JsonInventoryStore store;
    private readonly IFileHandler? files;
    private readonly IInventoryChangeTracker? inventoryChanges;

    public JsonInventoryMaintenanceService(
        JsonInventoryStore store,
        IInventoryChangeTracker? inventoryChanges = null,
        IFileHandler? files = null)
    {
        this.store = store;
        this.inventoryChanges = inventoryChanges;
        this.files = files;
    }

    public Task ReplaceAllPhotosWithSharedAssetsAsync(IProgress<MaintenanceProgress>? progress = null)
        => store.ReplaceAllPhotosWithSharedAssetsAsync(
            files ?? throw new InvalidOperationException("A file handler is required for photo maintenance."),
            progress);

    public async Task ResetAllDataAsync(IProgress<MaintenanceProgress>? progress = null)
    {
        await store.ResetAllDataAsync(
            files ?? throw new InvalidOperationException("A file handler is required for data reset."),
            progress);
        inventoryChanges?.MarkChanged();
    }

    /// <inheritdoc />
    public Task<bool> TryRecoverAsync() => store.TryRecoverAsync();

    /// <inheritdoc />
    public Task<bool> TryRollbackLastCommitAsync() => store.TryRollbackLastCommitAsync();
}
