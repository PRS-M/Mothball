using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;

namespace Infrastructure.Interfaces;

/// <summary>
/// Repository for the Item aggregate root, including hydration of photos and container relations.
/// </summary>
public interface IItemRepository
{
    Task<Item?> GetWithPhotosAsync(string itemId);
    Task<List<Item>> QueryWithPhotosAsync(ItemListSpecification specification);
    Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification);
    Task InsertAsync(Item item);
    Task UpdateAsync(Item item);
    Task DeletePhotoAsync(Item item, Guid imageId);
    Task DeleteAsync(string itemId);
}
