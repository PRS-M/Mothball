using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts;

namespace Infrastructure.Abstractions.Repositories;

/// <summary>
/// Repository for the Container aggregate root, including hydration of photos and item relations.
/// </summary>
public interface IContainerRepository
{
    /// <summary>
    /// Finds a container with an exact barcode value.
    /// </summary>
    /// <param name="barcodeValue">The barcode value to find.</param>
    Task<Container?> FindByBarcodeAsync(string barcodeValue);

    /// <summary>
    /// Gets a container by its string identifier.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    Task<Container?> GetAsync(string containerId);
    /// <param name="specification">The value used by the operation.</param>
    Task<List<Container>> QueryAsync(ContainerListSpecification specification);
    /// <summary>
    /// Gets the total item quantity stored in a container.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    Task<int> GetItemCountInContainerAsync(string containerId);
    /// <summary>
    /// Gets the number of distinct items stored in a container.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    Task<int> GetDistinctItemCountInContainerAsync(string containerId);
    /// <summary>
    /// Gets the container associated with an item.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    Task<Container?> GetContainerForItemAsync(string itemId);
    /// <param name="itemId">The identifier used by the operation.</param>
    Task<List<ItemContainerAllocation>> GetItemContainerAllocationsAsync(Guid itemId);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>>> GetItemContainerAllocationsAsync(
        IReadOnlyCollection<Guid> itemIds);
    /// <summary>
    /// Inserts a new container.
    /// </summary>
    /// <param name="container">The value used by the operation.</param>
    Task InsertAsync(Container container);
    /// <summary>
    /// Saves changes to a container.
    /// </summary>
    /// <param name="container">The value used by the operation.</param>
    Task UpdateAsync(Container container);
    /// <summary>
    /// Deletes a photo from a container.
    /// </summary>
    /// <param name="container">The value used by the operation.</param>
    /// <param name="imageId">The identifier used by the operation.</param>
    Task DeletePhotoAsync(Container container, Guid imageId);
    /// <summary>
    /// Deletes a container by its string identifier.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    Task DeleteAsync(string containerId);
}
