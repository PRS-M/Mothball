namespace Infrastructure.Services.JsonStore;

using CoreApp.Application.Contracts.Workspace;
using Infrastructure.Services.Startup;

public sealed class JsonStoreStartupInitializer : IAppStartupInitializer
{
    private readonly JsonInventoryStore store;
    private readonly IWorkspaceContext workspace;
    private readonly CanonicalInventoryMigrationService? migration;

    public JsonStoreStartupInitializer(JsonInventoryStore store)
        : this(store, new JsonWorkspaceContext(store))
    {
    }

    public JsonStoreStartupInitializer(JsonInventoryStore store, IWorkspaceContext workspace, CanonicalInventoryMigrationService? migration = null)
    {
        this.store = store;
        this.workspace = workspace;
        this.migration = migration;
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
        if (migration is not null) await migration.MigrateAsync().ConfigureAwait(false);
    }
}
