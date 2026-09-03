namespace Infrastructure.Services.JsonStore;

public sealed class JsonInventoryMaintenanceService : IInventoryMaintenanceService
{
    private readonly JsonInventoryStore store;

    public JsonInventoryMaintenanceService(JsonInventoryStore store)
    {
        this.store = store;
    }

    /// <inheritdoc />
    public Task<bool> TryRecoverAsync() => store.TryRecoverAsync();

    /// <inheritdoc />
    public Task<bool> TryRollbackLastCommitAsync() => store.TryRollbackLastCommitAsync();
}
