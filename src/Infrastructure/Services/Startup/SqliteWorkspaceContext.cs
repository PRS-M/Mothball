using CoreApp.Application.Contracts.Workspace;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Startup;

public sealed class SqliteWorkspaceContext(MothballDatabase database) : IWorkspaceContext
{
    public async Task<(Workspace Workspace, WorkspaceDefaults Defaults)> EnsureDefaultAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        var row = await database.Connection.FindAsync<DbWorkspace>(LocalWorkspaceDefaults.WorkspaceId).ConfigureAwait(false);
        var defaults = await database.Connection.FindAsync<DbWorkspaceDefaults>(LocalWorkspaceDefaults.WorkspaceId).ConfigureAwait(false);
        if (row is null || defaults is null)
        {
            var pair = LocalWorkspaceDefaults.Create(DateTimeOffset.UtcNow);
            await database.RunInTransactionAsync(connection =>
            {
                connection.InsertOrReplace(new DbWorkspace
                {
                    WorkspaceId = pair.Workspace.WorkspaceId, Name = pair.Workspace.Name, Kind = (int)pair.Workspace.Kind,
                    CreatedUtc = pair.Workspace.CreatedUtc, UpdatedUtc = pair.Workspace.UpdatedUtc, Version = pair.Workspace.Version,
                });
                connection.InsertOrReplace(new DbWorkspaceDefaults
                {
                    WorkspaceId = pair.Defaults.WorkspaceId, DefaultWarehouseId = pair.Defaults.DefaultWarehouseId,
                    UnassignedLocationId = pair.Defaults.UnassignedLocationId, DefaultUnitOfMeasureId = pair.Defaults.DefaultUnitOfMeasureId,
                    DefaultStockStatusId = pair.Defaults.DefaultStockStatusId,
                });
            }).ConfigureAwait(false);
            return pair;
        }

        return (new Workspace(row.WorkspaceId, row.Name, (WorkspaceKind)row.Kind, row.CreatedUtc, row.UpdatedUtc, row.Version),
            new WorkspaceDefaults(defaults.WorkspaceId, defaults.DefaultWarehouseId, defaults.UnassignedLocationId, defaults.DefaultUnitOfMeasureId, defaults.DefaultStockStatusId));
    }
}
