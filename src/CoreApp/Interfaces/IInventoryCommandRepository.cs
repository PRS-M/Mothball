using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;

namespace CoreApp.Interfaces;

public interface IInventoryCommandRepository
{
    Task InsertContainerAsync(Container container);

    Task InsertItemAsync(Item item);

    Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId);

    Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity);

    Task UpdateContainerAsync(Container container);

    Task UpdateItemAsync(Item item);

    Task UpdateImageItemAsync(ImageItem image, Guid ownerId);

    Task DeleteItemAsync(string itemId);

    Task DeleteContainerAsync(string containerId);
}