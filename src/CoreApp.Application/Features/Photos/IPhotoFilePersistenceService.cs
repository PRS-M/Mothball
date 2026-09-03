using CoreApp.Domain.ValueObjects;

namespace CoreApp.Application.Features.Photos;

/// <summary>
/// Defines the workflow for persisting photo bytes and metadata.
/// </summary>
public interface IPhotoFilePersistenceService
{
    /// <summary>
    /// Saves photo bytes and coordinates persistence of their image metadata.
    /// </summary>
    /// <param name="bytes">The encoded photo bytes to save.</param>
    /// <param name="addImageItem">Adds image metadata and returns the new image item.</param>
    /// <param name="removeImageItem">Removes image metadata when persistence fails.</param>
    /// <param name="saveDirectory">The directory in which to save the photo file.</param>
    /// <param name="persistAsync">Persists the image metadata after the file is saved.</param>
    Task<int> PersistPhotoBytesAsync(
        byte[] bytes,
        Func<ImageItem> addImageItem,
        Action<Guid> removeImageItem,
        string saveDirectory,
        Func<ImageItem, Task> persistAsync);
}
