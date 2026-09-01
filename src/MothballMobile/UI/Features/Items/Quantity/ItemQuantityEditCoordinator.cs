using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Inventory;
using MothballMobile.UI.Features.Items.ItemDetails;

namespace MothballMobile.UI.Features.Items.Quantity;

public enum ItemQuantityDecreasePreference
{
    AssignedFirst,
    UnassignedFirst,
}

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
        Guid? preferredContainerId = null,
        ItemQuantityDecreasePreference decreasePreference = ItemQuantityDecreasePreference.AssignedFirst)
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

        var withdrawal = decreasePreference == ItemQuantityDecreasePreference.UnassignedFirst
            ? await ExecuteUnassignedFirstAsync(inventory, selectedQuantity.Value, preferredContainerId)
            : await withdrawalCoordinator.ExecuteAsync(
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

    private async Task<ItemInventoryWithdrawalExecutionResult?> ExecuteUnassignedFirstAsync(
        InventorySnapshot inventory,
        int requestedTotal,
        Guid? preferredContainerId)
    {
        int requestedDecrease = inventory.TotalQuantity - requestedTotal;
        int unassignedWithdrawal = Math.Min(requestedDecrease, inventory.UnassignedQuantity);
        if (unassignedWithdrawal == requestedDecrease)
        {
            var plan = ItemInventoryConsumptionPlanner.Plan(
                inventory,
                ItemInventoryConsumptionSource.FromUnassigned(),
                unassignedWithdrawal);
            var update = await inventoryCommands.ApplyWithdrawalAsync(inventory.Item.ItemId, plan);
            return new ItemInventoryWithdrawalExecutionResult(plan, update);
        }

        var inventoryAfterUnassignedWithdrawal = new InventorySnapshot(
            inventory.Item,
            inventory.TotalQuantity - unassignedWithdrawal,
            inventory.AssignedQuantity,
            inventory.Allocations);
        return await withdrawalCoordinator.ExecuteAsync(
            inventoryAfterUnassignedWithdrawal,
            requestedTotal,
            preferredContainerId);
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
