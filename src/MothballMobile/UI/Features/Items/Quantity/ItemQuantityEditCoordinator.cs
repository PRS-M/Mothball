using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.InventoryAggregate;
using MothballMobile.UI.Features.Items.ItemDetails;

namespace MothballMobile.UI.Features.Items.Quantity;

public sealed class ItemQuantityEditCoordinator
{
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IItemInventoryCommandService inventoryCommands;
    private readonly ItemInventoryWithdrawalCoordinator withdrawalCoordinator;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;

    public ItemQuantityEditCoordinator(
        IItemDetailsQueryHandler itemDetailsQueries,
        IItemInventoryCommandService inventoryCommands,
        ItemInventoryWithdrawalCoordinator withdrawalCoordinator,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions)
    {
        this.itemDetailsQueries = itemDetailsQueries;
        this.inventoryCommands = inventoryCommands;
        this.withdrawalCoordinator = withdrawalCoordinator;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
    }

    public async Task<ItemQuantityEditExecutionResult?> ExecuteAsync(
        Guid itemId,
        Guid? preferredContainerId = null)
    {
        var details = await itemDetailsQueries.GetDetailsAsync(itemId.ToString());
        if (details is null)
        {
            return null;
        }

        var inventory = details.Inventory;
        var selectedQuantity = await popup.PickNumberAsync(
            popupDefinitions.SetTotalQuantity(inventory.TotalQuantity, inventory.AssignedQuantity));
        if (selectedQuantity is null || selectedQuantity.Value == inventory.TotalQuantity)
        {
            return null;
        }

        if (selectedQuantity.Value == 0)
        {
            if (!await popup.ConfirmAsync(
                    popupDefinitions.DeleteItemBySettingTotalToZero(inventory.Item.Name)))
            {
                return null;
            }

            var deleted = await inventoryCommands.ApplyWithdrawalAsync(
                itemId,
                new ItemInventoryWithdrawalPlan(0, 0, 0, [], true));
            return new ItemQuantityEditExecutionResult(deleted, null);
        }

        if (selectedQuantity.Value > inventory.TotalQuantity)
        {
            var increased = await inventoryCommands.IncreaseTotalQuantityAsync(itemId, selectedQuantity.Value);
            return new ItemQuantityEditExecutionResult(
                increased,
                ToSnapshot(inventory, increased, inventory.Allocations));
        }

        var withdrawal = await withdrawalCoordinator.ExecuteAsync(
            inventory,
            selectedQuantity.Value,
            preferredContainerId);
        return withdrawal is null
            ? null
            : new ItemQuantityEditExecutionResult(
                withdrawal.Update,
                withdrawal.Update.ItemDeleted
                    ? null
                    : ToSnapshot(inventory, withdrawal.Update, withdrawal.Plan.Allocations));
    }

    private static InventorySnapshot ToSnapshot(
        InventorySnapshot before,
        ItemInventoryUpdateResult update,
        IReadOnlyList<ItemContainerAllocation> allocations)
        => new(before.Item, update.TotalQuantity, update.AssignedQuantity, allocations.Where(a => a.Quantity > 0).ToList());
}

public sealed record ItemQuantityEditExecutionResult(
    ItemInventoryUpdateResult Update,
    InventorySnapshot? Inventory);
