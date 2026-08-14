using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Interfaces;

public interface IPhotoDeletionService
{
    Task<bool> DeleteContainerPhotoAsync(Container container, Guid imageId);

    Task<bool> DeleteItemPhotoAsync(Item item, Guid imageId);
}
