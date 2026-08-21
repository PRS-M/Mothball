using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Application.Features.Containers.Queries;
using CoreApp.Application.Features.Inventory.Allocation;

namespace CoreApp.Application.Features.Containers.ContainerDetails;

public sealed class ContainerDetailsHandler : IContainerDetailsHandler
{
    private readonly IContainerDetailsQueryHandler containerDetailsQueries;
    private readonly IContainerItemQuantityService quantityService;

    public ContainerDetailsHandler(
        IContainerDetailsQueryHandler containerDetailsQueries,
        IContainerItemQuantityService quantityService)
    {
        this.containerDetailsQueries = containerDetailsQueries ?? throw new ArgumentNullException(nameof(containerDetailsQueries));
        this.quantityService = quantityService ?? throw new ArgumentNullException(nameof(quantityService));
    }

    public async Task<ContainerDetailsSummary?> GetSummaryAsync(string containerId)
    {
        var details = await containerDetailsQueries.GetDetailsAsync(containerId);
        if (details is null)
        {
            return null;
        }

        var itemTypesCount = await containerDetailsQueries.GetDistinctItemCountAsync(containerId);
        return new ContainerDetailsSummary(details.Container, itemTypesCount, details.TotalItemCount);
    }

    public async Task<ContainerDetailsQuantityUpdate> SaveItemQuantityAsync(
        Container container,
        Guid itemId,
        int quantity)
    {
        ArgumentNullException.ThrowIfNull(container);

        var quantityUpdate = await quantityService.SaveQuantityAsync(container, itemId, quantity);
        var itemTypesCount = await containerDetailsQueries.GetDistinctItemCountAsync(container.ContainerId.ToString());
        return new ContainerDetailsQuantityUpdate(
            new ContainerDetailsSummary(container, itemTypesCount, quantityUpdate.TotalItemCount),
            quantityUpdate.Removed,
            quantityUpdate.TotalQuantity,
            quantityUpdate.AssignedQuantity,
            quantityUpdate.UnassignedQuantity);
    }
}