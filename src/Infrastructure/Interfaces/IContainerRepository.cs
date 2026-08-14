using CoreApp.Entities.ContainerAggregate;
using CoreApp.Specifications;

namespace Infrastructure.Interfaces;

/// <summary>
/// Repository for the Container aggregate root, including hydration of photos and item relations.
/// </summary>
public interface IContainerRepository
{
    Task<Container?> GetAsync(string containerId);
    Task<List<Container>> QueryAsync(ContainerListSpecification specification);
    Task<int> GetItemCountInContainerAsync(string containerId);
    Task<Container?> GetContainerForItemAsync(string itemId);
    Task InsertAsync(Container container);
    Task UpdateAsync(Container container);
    Task DeletePhotoAsync(Container container, Guid imageId);
    Task DeleteAsync(string containerId);
}
