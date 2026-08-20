using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Containers.ContainerDetails;

public interface IContainerDetailsHandler
{
    Task<ContainerDetailsSummary?> GetSummaryAsync(string containerId);

    Task<ContainerDetailsQuantityUpdate> SaveItemQuantityAsync(Container container, Guid itemId, int quantity);
}