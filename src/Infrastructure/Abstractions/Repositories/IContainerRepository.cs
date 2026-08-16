using CoreApp.Entities.Inventory;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Specifications;
using CoreApp.Contracts;

namespace Infrastructure.Abstractions.Repositories;

/// <summary>
/// Repository for the Container aggregate root, including hydration of photos and item relations.
/// </summary>
public interface IContainerRepository
{
    Task<Container?> GetAsync(string containerId);
    Task<List<Container>> QueryAsync(ContainerListSpecification specification);
    Task<int> GetItemCountInContainerAsync(string containerId);
    Task<int> GetDistinctItemCountInContainerAsync(string containerId);
    Task<Container?> GetContainerForItemAsync(string itemId);
    Task<List<ItemContainerAllocation>> GetItemContainerAllocationsAsync(Guid itemId);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>>> GetItemContainerAllocationsAsync(
        IReadOnlyCollection<Guid> itemIds);
    Task InsertAsync(Container container);
    Task UpdateAsync(Container container);
    Task DeletePhotoAsync(Container container, Guid imageId);
    Task DeleteAsync(string containerId);
}
