using CoreApp.Contracts;
using CoreApp.Interfaces;

namespace CoreApp.Services;

public sealed class ItemDetailsQueryHandler : IItemDetailsQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ItemDetailsQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
    }

    public async Task<ItemDetailsResult?> GetDetailsAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var parsedItemId))
        {
            return null;
        }

        var summary = await inventoryQueries.GetItemInventorySummaryAsync(parsedItemId);
        return summary is null ? null : new ItemDetailsResult(summary);
    }
}
