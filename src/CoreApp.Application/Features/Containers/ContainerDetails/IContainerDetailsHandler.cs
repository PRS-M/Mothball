using CoreApp.Application.Contracts;
using CoreApp.Domain.Entities.ContainerAggregate;

namespace CoreApp.Application.Features.Containers.ContainerDetails;

public interface IContainerDetailsHandler
{
    Task<ContainerDetailsSummary?> GetSummaryAsync(string containerId);

    Task<ContainerDetailsQuantityUpdate> SaveItemQuantityAsync(Container container, Guid itemId, int quantity);
}