namespace Infrastructure.Services.JsonStore;

using CoreApp.Application.Contracts.Workspace;

public sealed class JsonStoreStartupInitializer : IAppStartupInitializer
{
    private readonly JsonInventoryStore store;
    private readonly IWorkspaceContext workspace;

    public JsonStoreStartupInitializer(JsonInventoryStore store)
        : this(store, new JsonWorkspaceContext(store))
    {
    }

    public JsonStoreStartupInitializer(JsonInventoryStore store, IWorkspaceContext workspace)
    {
        this.store = store;
        this.workspace = workspace;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var recovered = await store.TryRecoverAsync().ConfigureAwait(false);
        if (!recovered)
        {
            throw new InvalidOperationException("Failed to recover JSON inventory store during startup.");
        }

        await workspace.EnsureDefaultAsync().ConfigureAwait(false);
    }
}
