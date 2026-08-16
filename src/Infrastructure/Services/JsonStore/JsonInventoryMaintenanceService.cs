using System.Threading.Tasks;

namespace Infrastructure.Services.JsonStore;

public sealed class JsonInventoryMaintenanceService : IInventoryMaintenanceService
{
    private readonly JsonInventoryStore store;

    public JsonInventoryMaintenanceService(JsonInventoryStore store)
    {
        this.store = store;
    }

    public Task<bool> TryRecoverAsync() => store.TryRecoverAsync();

    public Task<bool> TryRollbackLastCommitAsync() => store.TryRollbackLastCommitAsync();
}
