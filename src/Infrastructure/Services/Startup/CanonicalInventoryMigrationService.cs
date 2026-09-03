using System.Security.Cryptography;
using System.Text;
using CoreApp.Application.Features.Inventory;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts.Workspace;
using CoreApp.Domain.Entities.InventoryAggregate;

namespace Infrastructure.Services.Startup;

/// <summary>Backfills legacy inventory into canonical balances exactly once per placement.</summary>
public sealed class CanonicalInventoryMigrationService(
    IItemRepository items,
    IItemInventoryRepository inventories,
    IWorkspaceContext workspace,
    CanonicalInventoryCommandService canonical)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var defaults = (await workspace.EnsureDefaultAsync(cancellationToken).ConfigureAwait(false)).Defaults;
        var workspaceId = new InventoryWorkspaceId(defaults.WorkspaceId);
        var allItems = await items.QueryWithPhotosAsync(new(ItemQueryFilter.All)).ConfigureAwait(false);
        var inventoryByItem = await inventories.GetManyAsync(allItems.Select(x => x.ItemId).ToList()).ConfigureAwait(false);
        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!inventoryByItem.TryGetValue(item.ItemId, out var inventory)) continue;
            foreach (var allocation in inventory.Allocations)
            {
                var placement = new InventoryPlacementId(allocation.ContainerId);
                await canonical.EnsureOpeningBalanceAsync(workspaceId, item.ItemId, placement, allocation.Quantity, MigrationOperationId(item.ItemId, placement), cancellationToken).ConfigureAwait(false);
            }

            var unassigned = new InventoryPlacementId(defaults.UnassignedLocationId);
            await canonical.EnsureOpeningBalanceAsync(workspaceId, item.ItemId, unassigned, inventory.UnassignedQuantity, MigrationOperationId(item.ItemId, unassigned), cancellationToken).ConfigureAwait(false);
        }
    }

    private static Guid MigrationOperationId(Guid itemId, InventoryPlacementId placement)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"mothball:opening-balance:{itemId:N}:{placement.Value:N}"));
        return new Guid(bytes);
    }
}
