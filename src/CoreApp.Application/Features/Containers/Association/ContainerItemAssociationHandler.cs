using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Containers.Commands;
using CoreApp.Application.Features.Items.Queries;

namespace CoreApp.Application.Features.Containers.Association;

public sealed class ContainerItemAssociationHandler : IContainerItemAssociationHandler
{
    private readonly IItemDetailsQueryHandler itemDetailsQueries;
    private readonly IAssignItemToContainerCommandHandler assignItemToContainer;

    public ContainerItemAssociationHandler(
        IItemDetailsQueryHandler itemDetailsQueries,
        IAssignItemToContainerCommandHandler assignItemToContainer)
    {
        this.itemDetailsQueries = itemDetailsQueries ?? throw new ArgumentNullException(nameof(itemDetailsQueries));
        this.assignItemToContainer = assignItemToContainer ?? throw new ArgumentNullException(nameof(assignItemToContainer));
    }

    public async Task<int> GetAvailableQuantityAsync(
        Guid itemId,
        Guid containerId,
        int fallbackUnassignedQuantity)
    {
        var details = await itemDetailsQueries.GetDetailsAsync(itemId.ToString());
        if (details is null)
        {
            return fallbackUnassignedQuantity;
        }

        var currentContainerQuantity = details.Inventory.Allocations
            .FirstOrDefault(allocation => allocation.ContainerId == containerId)?.Quantity ?? 0;
        return details.Inventory.UnassignedQuantity + currentContainerQuantity;
    }

    public async Task<ContainerItemAssociationResult> TryAssociateAsync(
        Guid itemId,
        Guid containerId,
        int quantity,
        int fallbackUnassignedQuantity)
    {
        var availableQuantity = await GetAvailableQuantityAsync(itemId, containerId, fallbackUnassignedQuantity);
        if (quantity <= 0 || quantity > availableQuantity)
        {
            return new ContainerItemAssociationResult(Associated: false, availableQuantity);
        }

        await assignItemToContainer.AssignAsync(itemId, containerId, quantity);
        return new ContainerItemAssociationResult(Associated: true, availableQuantity);
    }
}