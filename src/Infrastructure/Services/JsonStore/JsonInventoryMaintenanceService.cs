namespace Infrastructure.Services.JsonStore;

public sealed class JsonInventoryMaintenanceService : IInventoryMaintenanceService
{
    private readonly JsonInventoryStore store;
    private readonly IFileHandler? files;

    public JsonInventoryMaintenanceService(JsonInventoryStore store, IFileHandler? files = null)
    {
        this.store = store;
        this.files = files;
    }

    public Task ReplaceAllPhotosWithSharedAssetsAsync(IProgress<MaintenanceProgress>? progress = null)
        => store.ReplaceAllPhotosWithSharedAssetsAsync(
            files ?? throw new InvalidOperationException("A file handler is required for photo maintenance."),
            progress);

    public Task ResetAllDataAsync(IProgress<MaintenanceProgress>? progress = null)
        => store.ResetAllDataAsync(
            files ?? throw new InvalidOperationException("A file handler is required for data reset."),
            progress);

    /// <inheritdoc />
    public Task<bool> TryRecoverAsync() => store.TryRecoverAsync();

    /// <inheritdoc />
    public Task<bool> TryRollbackLastCommitAsync() => store.TryRollbackLastCommitAsync();
}
