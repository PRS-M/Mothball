using CoreApp.Entities.Shared;

namespace Infrastructure.Interfaces;

/// <summary>
/// Simple repository for image CRUD operations.
/// </summary>
public interface IImageRepository
{
    Task InsertAsync(ImageItem imageItem, Guid ownerId);
    Task UpdateAsync(ImageItem image, Guid ownerId);
    Task DeleteAsync(Guid imageId, Guid ownerId);
}
