using CoreApp.Domain.ValueObjects;

namespace Infrastructure.Abstractions.Repositories;

/// <summary>
/// Simple repository for image CRUD operations.
/// </summary>
public interface IImageRepository
{
    /// <summary>
    /// Inserts an image and associates it with an owner.
    /// </summary>
    /// <param name="imageItem">The value used by the operation.</param>
    /// <param name="ownerId">The identifier used by the operation.</param>
    Task InsertAsync(ImageItem imageItem, Guid ownerId);
    /// <summary>
    /// Saves changes to an image associated with an owner.
    /// </summary>
    /// <param name="image">The value used by the operation.</param>
    /// <param name="ownerId">The identifier used by the operation.</param>
    Task UpdateAsync(ImageItem image, Guid ownerId);
    /// <summary>
    /// Deletes an image associated with an owner.
    /// </summary>
    /// <param name="imageId">The identifier used by the operation.</param>
    /// <param name="ownerId">The identifier used by the operation.</param>
    Task DeleteAsync(Guid imageId, Guid ownerId);
}
