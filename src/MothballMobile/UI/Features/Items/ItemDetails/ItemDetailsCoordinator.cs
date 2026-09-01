using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using Microsoft.Extensions.Logging;
using MothballMobile.UI.Features.Items.Consumption;
using MothballMobile.UI.Features.Items.Quantity;

namespace MothballMobile.UI.Features.Items.ItemDetails;

public sealed class ItemDetailsCoordinator
{
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IDeleteItemCommandHandler deleteItemHandler;
    private readonly IUpdateItemDescriptionCommandHandler updateItemDescriptionHandler;
    private readonly ItemConsumptionCoordinator consumptionCoordinator;
    private readonly ItemQuantityEditCoordinator quantityEditCoordinator;
    private readonly ILogger<ItemDetailsCoordinator> logger;

    public ItemDetailsCoordinator(
        IItemDetailsQueryHandler itemDetailsQueries,
        IDeleteItemCommandHandler deleteItemHandler,
        IUpdateItemDescriptionCommandHandler updateItemDescriptionHandler,
        ItemConsumptionCoordinator consumptionCoordinator,
        ItemQuantityEditCoordinator quantityEditCoordinator,
        ILogger<ItemDetailsCoordinator> logger)
    {
        this.itemDetailsQueries = itemDetailsQueries;
        this.deleteItemHandler = deleteItemHandler;
        this.updateItemDescriptionHandler = updateItemDescriptionHandler;
        this.consumptionCoordinator = consumptionCoordinator;
        this.quantityEditCoordinator = quantityEditCoordinator;
        this.logger = logger;
    }

    public Task<ItemDetailsResult?> GetDetailsAsync(string itemId)
        => itemDetailsQueries.GetDetailsAsync(itemId);

    public Task UpdateDescriptionAsync(Item item, string description)
        => updateItemDescriptionHandler.UpdateAsync(item, description);

    public Task DeleteItemAsync(string itemId)
        => deleteItemHandler.DeleteAsync(itemId);

    public Task<ItemConsumptionExecutionResult?> ConsumeAsync(Guid itemId, Guid? preferredContainerId)
    {
        logger.LogDebug("Routing item use request to source-specific consumption workflow.");
        return consumptionCoordinator.ExecuteAsync(itemId, preferredContainerId);
    }

    public Task<ItemQuantityEditExecutionResult?> EditQuantityAsync(Guid itemId, Guid? preferredContainerId)
    {
        logger.LogDebug("Routing item quantity request to target-total workflow.");
        return quantityEditCoordinator.ExecuteAsync(itemId, preferredContainerId);
    }
}
