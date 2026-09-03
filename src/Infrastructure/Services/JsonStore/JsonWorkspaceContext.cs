using CoreApp.Application.Contracts.Workspace;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore;

public sealed class JsonWorkspaceContext(JsonInventoryStore store) : IWorkspaceContext
{
    public async Task<(Workspace Workspace, WorkspaceDefaults Defaults)> EnsureDefaultAsync(CancellationToken cancellationToken = default)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var existing = state.Workspaces.FirstOrDefault(x => x.WorkspaceId == LocalWorkspaceDefaults.WorkspaceId);
        if (existing is not null)
            return Map(existing);

        var pair = LocalWorkspaceDefaults.Create(DateTimeOffset.UtcNow);
        await store.UpdateAsync(current =>
        {
            current.Workspaces.Add(new JsonWorkspaceRow
            {
                WorkspaceId = pair.Workspace.WorkspaceId, Name = pair.Workspace.Name, Kind = (int)pair.Workspace.Kind,
                CreatedUtc = pair.Workspace.CreatedUtc, UpdatedUtc = pair.Workspace.UpdatedUtc, Version = pair.Workspace.Version,
                DefaultWarehouseId = pair.Defaults.DefaultWarehouseId, UnassignedLocationId = pair.Defaults.UnassignedLocationId,
                DefaultUnitOfMeasureId = pair.Defaults.DefaultUnitOfMeasureId, DefaultStockStatusId = pair.Defaults.DefaultStockStatusId,
            });
            return Task.CompletedTask;
        }).ConfigureAwait(false);
        return pair;
    }

    private static (Workspace Workspace, WorkspaceDefaults Defaults) Map(JsonWorkspaceRow row)
        => (new(row.WorkspaceId, row.Name, (WorkspaceKind)row.Kind, row.CreatedUtc, row.UpdatedUtc, row.Version),
            new WorkspaceDefaults(row.WorkspaceId, row.DefaultWarehouseId, row.UnassignedLocationId, row.DefaultUnitOfMeasureId, row.DefaultStockStatusId));
}
