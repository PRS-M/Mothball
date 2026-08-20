using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;

namespace CoreApp.Application.Features.Photos;

/// <summary>
/// Defines operations for deleting persisted photos.
/// </summary>
public interface IPhotoDeletionService
{
    /// <summary>
    /// Deletes a photo from a container and its associated storage.
    /// </summary>
    /// <param name="container">The value used by the operation.</param>
    /// <param name="imageId">The identifier used by the operation.</param>
    Task<bool> DeleteContainerPhotoAsync(Container container, Guid imageId);

    /// <summary>
    /// Deletes a photo from an item and its associated storage.
    /// </summary>
    /// <param name="item">The value used by the operation.</param>
    /// <param name="imageId">The identifier used by the operation.</param>
    Task<bool> DeleteItemPhotoAsync(Item item, Guid imageId);

    /// <summary>
    /// Attempts to delete all photo files belonging to an item without failing the caller.
    /// </summary>
    /// <param name="item">The value used by the operation.</param>
    Task DeleteItemPhotoFilesBestEffortAsync(Item item);
}
