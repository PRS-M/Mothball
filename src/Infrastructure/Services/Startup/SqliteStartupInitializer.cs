namespace Infrastructure.Services.Startup;

using CoreApp.Application.Contracts.Workspace;

public sealed class SqliteStartupInitializer : IAppStartupInitializer
{
    private readonly MothballDatabase database;
    private readonly IWorkspaceContext workspace;
    private readonly CanonicalInventoryMigrationService? migration;

    public SqliteStartupInitializer(MothballDatabase database)
        : this(database, new SqliteWorkspaceContext(database))
    {
    }

    public SqliteStartupInitializer(MothballDatabase database, IWorkspaceContext workspace, CanonicalInventoryMigrationService? migration = null)
    {
        this.database = database;
        this.workspace = workspace;
        this.migration = migration;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await database.InitializeAsync().ConfigureAwait(false);
        await workspace.EnsureDefaultAsync().ConfigureAwait(false);
        if (migration is not null) await migration.MigrateAsync().ConfigureAwait(false);
    }
}
