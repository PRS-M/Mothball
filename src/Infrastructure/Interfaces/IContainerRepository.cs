using CoreApp.Entities.ContainerAggregate;

namespace Infrastructure.Interfaces;

/// <summary>
/// Repository for the Container aggregate root, including hydration of photos and item relations.
/// </summary>
public interface IContainerRepository
{
    Task<Container?> GetAsync(string containerId);
    Task<List<Container>> GetAllAsync();
    Task<List<Container>> GetAllAsync(int pageNumber, int pageSize);
    Task<List<Container>> GetEmptyAsync(int pageNumber, int pageSize);
    Task<List<Container>> SearchAsync(string searchTerm);
    Task<List<Container>> SearchEmptyAsync(string searchTerm);
    Task<Container?> GetWithItemsAndPhotosAsync(string containerId);
    Task<Container?> GetWithItemsAndPhotosAsync(string containerId, int pageNumber, int pageSize);
    Task<int> GetItemCountInContainerAsync(string containerId);
    Task<Container?> GetContainerForItemAsync(string itemId);
    Task InsertAsync(Container container);
    Task UpdateAsync(Container container);
    Task DeletePhotoAsync(Container container, Guid imageId);
    Task DeleteAsync(string containerId);
}
