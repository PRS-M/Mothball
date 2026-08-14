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
        var item = await inventoryQueries.GetItemWithPhotosAsync(itemId);
        if (item is null)
        {
            return null;
        }

        var container = await inventoryQueries.GetContainerForItemAsync(item.ItemId.ToString());
        return new ItemDetailsResult(item, container?.ContainerId);
    }
}
