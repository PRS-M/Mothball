namespace CoreApp.Application.Features.Containers.Association;

public interface IContainerItemAssociationHandler
{
    Task<int> GetAvailableQuantityAsync(Guid itemId, Guid containerId, int fallbackUnassignedQuantity);

    Task<ContainerItemAssociationResult> TryAssociateAsync(
        Guid itemId,
        Guid containerId,
        int quantity,
        int fallbackUnassignedQuantity);
}