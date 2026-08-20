using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Microsoft.Extensions.Logging;

namespace MothballMobile.UI.Features.Items.ItemDetails;

public sealed class ItemDetailsCoordinator
{
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IItemInventoryCommandService inventoryCommands;
    private readonly IDeleteItemCommandHandler deleteItemHandler;
    private readonly IUpdateItemDescriptionCommandHandler updateItemDescriptionHandler;
    private readonly ItemInventoryWithdrawalCoordinator withdrawalCoordinator;
    private readonly ILogger<ItemDetailsCoordinator> logger;

    public ItemDetailsCoordinator(
        IItemDetailsQueryHandler itemDetailsQueries,
        IItemInventoryCommandService inventoryCommands,
        IDeleteItemCommandHandler deleteItemHandler,
        IUpdateItemDescriptionCommandHandler updateItemDescriptionHandler,
        ItemInventoryWithdrawalCoordinator withdrawalCoordinator,
        ILogger<ItemDetailsCoordinator> logger)
    {
        this.itemDetailsQueries = itemDetailsQueries;
        this.inventoryCommands = inventoryCommands;
        this.deleteItemHandler = deleteItemHandler;
        this.updateItemDescriptionHandler = updateItemDescriptionHandler;
        this.withdrawalCoordinator = withdrawalCoordinator;
        this.logger = logger;
    }

    public Task<ItemDetailsResult?> GetDetailsAsync(string itemId)
        => itemDetailsQueries.GetDetailsAsync(itemId);

    public Task UpdateDescriptionAsync(Item item, string description)
        => updateItemDescriptionHandler.UpdateAsync(item, description);

    public Task DeleteItemAsync(string itemId)
        => deleteItemHandler.DeleteAsync(itemId);

    public Task<ItemInventoryUpdateResult> DeleteBySettingTotalToZeroAsync(Item item)
        => inventoryCommands.ApplyWithdrawalAsync(
            item.ItemId,
            new ItemInventoryWithdrawalPlan(0, 0, 0, [], true));

    public async Task<ItemInventoryUpdateResult> IncreaseTotalQuantityAsync(Item item, int selectedQuantity)
    {
        logger.LogDebug("Routing item total request to increase command.");
        return await inventoryCommands.IncreaseTotalQuantityAsync(item.ItemId, selectedQuantity);
    }

    public Task<ItemInventoryWithdrawalExecutionResult?> WithdrawAsync(
        InventorySnapshot inventory,
        int requestedTotal,
        Guid? preferredContainerId)
    {
        logger.LogDebug("Routing item total request to withdrawal workflow.");
        return withdrawalCoordinator.ExecuteAsync(inventory, requestedTotal, preferredContainerId);
    }
}