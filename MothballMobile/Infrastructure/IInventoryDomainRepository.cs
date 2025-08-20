using CoreApp.Models;

namespace MothballMobile.Infrastructure;

public interface IInventoryDomainRepository
{
    Task<Container?> GetContainerAsync(string containerId);
    Task<List<Item>> GetItemsForContainerAsync(string containerId);
    Task<(Container container, List<Item> items)?> GetContainerWithItemsAndPhotosAsync(string containerId);
    Task<Item?> GetItemWithPhotosAsync(string itemId);
}
