using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Items.Queries;

public sealed class ItemDetailsQueryHandler : IItemDetailsQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ItemDetailsQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

    /// <inheritdoc />
    public async Task<ItemDetailsResult?> GetDetailsAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var parsedItemId))
        {
            return null;
        }

        var summary = await inventoryQueries.GetInventorySnapshotAsync(parsedItemId);
        return summary is null ? null : new ItemDetailsResult(summary);
    }
}
