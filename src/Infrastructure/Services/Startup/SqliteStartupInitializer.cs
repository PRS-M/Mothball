namespace Infrastructure.Services.Startup;

using CoreApp.Application.Contracts.Workspace;

public sealed class SqliteStartupInitializer : IAppStartupInitializer
{
    private readonly MothballDatabase database;
    private readonly IWorkspaceContext workspace;

    public SqliteStartupInitializer(MothballDatabase database, IWorkspaceContext workspace)
    {
        this.database = database;
        this.workspace = workspace;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await database.InitializeAsync().ConfigureAwait(false);
        await workspace.EnsureDefaultAsync().ConfigureAwait(false);
    }
}
