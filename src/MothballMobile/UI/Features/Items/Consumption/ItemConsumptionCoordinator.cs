using CoreApp.Domain.Entities.InventoryAggregate;

namespace MothballMobile.UI.Features.Items.Consumption;

public sealed class ItemConsumptionCoordinator
{
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IItemInventoryCommandService inventoryCommands;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;

    public ItemConsumptionCoordinator(
        IItemDetailsQueryHandler itemDetailsQueries,
        IItemInventoryCommandService inventoryCommands,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions)
    {
        this.itemDetailsQueries = itemDetailsQueries;
        this.inventoryCommands = inventoryCommands;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
    }

    public async Task<ItemConsumptionExecutionResult?> ExecuteAsync(
        Guid itemId,
        Guid? preferredContainerId = null)
    {
        var details = await itemDetailsQueries.GetDetailsAsync(itemId.ToString());
        if (details is null)
        {
            return null;
        }

        var inventory = details.Inventory;
        var source = await SelectSourceAsync(inventory, preferredContainerId);
        if (source is null)
        {
            return null;
        }

        var quantity = await SelectQuantityAsync(inventory, source);
        if (quantity is null)
        {
            return null;
        }

        if (quantity.Value == inventory.TotalQuantity
            && !await popup.ConfirmAsync(popupDefinitions.ConfirmFinalStockConsumption(inventory.Item.Name)))
        {
            return null;
        }

        var update = await inventoryCommands.ConsumeAsync(itemId, source, quantity.Value);
        if (update.ItemDeleted)
        {
            return new ItemConsumptionExecutionResult(update, null);
        }

        var refreshed = await itemDetailsQueries.GetDetailsAsync(itemId.ToString())
            ?? throw new KeyNotFoundException($"Item '{itemId}' was not found after consumption.");
        return new ItemConsumptionExecutionResult(update, refreshed.Inventory);
    }

    private async Task<ItemInventoryConsumptionSource?> SelectSourceAsync(
        InventorySnapshot inventory,
        Guid? preferredContainerId)
    {
        var preferred = preferredContainerId is null
            ? null
            : inventory.Allocations.FirstOrDefault(allocation =>
                allocation.ContainerId == preferredContainerId.Value && allocation.Quantity > 0);

        if (preferred is not null
            && await popup.ConfirmAsync(popupDefinitions.ConfirmPreferredConsumptionSource(preferred)))
        {
            return ItemInventoryConsumptionSource.FromContainer(preferred.ContainerId);
        }

        return await popup.SelectOptionAsync(popupDefinitions.ConsumptionSourcePicker(inventory));
    }

    private Task<int?> SelectQuantityAsync(
        InventorySnapshot inventory,
        ItemInventoryConsumptionSource source)
    {
        if (source.Kind == ItemInventoryConsumptionSourceKind.Unassigned)
        {
            return popup.PickNumberAsync(
                popupDefinitions.ConsumeUnassignedQuantity(inventory.UnassignedQuantity));
        }

        var allocation = inventory.Allocations.Single(candidate => candidate.ContainerId == source.ContainerId);
        return popup.PickNumberAsync(popupDefinitions.ConsumeFromContainer(allocation));
    }
}

public sealed record ItemConsumptionExecutionResult(
    ItemInventoryUpdateResult Update,
    InventorySnapshot? Inventory);
