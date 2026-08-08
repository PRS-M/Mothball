using System.Threading.Tasks;
using CoreApp.Interfaces;

namespace Infrastructure.Services.JsonStore;

public sealed class JsonStoreStartupInitializer : IAppStartupInitializer
{
    private readonly JsonInventoryStore store;

    public JsonStoreStartupInitializer(JsonInventoryStore store)
    {
        this.store = store;
    }

    public async Task InitializeAsync()
    {
        var recovered = await store.TryRecoverAsync().ConfigureAwait(false);
        if (!recovered)
        {
            throw new InvalidOperationException("Failed to recover JSON inventory store during startup.");
        }
    }
}
