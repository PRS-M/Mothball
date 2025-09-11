using CoreApp.Entities.Shared;

namespace CoreApp.Interfaces;

/// <summary>
/// Provides UI-friendly file system paths for image value objects while hiding
/// infrastructure concerns (app data root, folder names, etc.) from ViewModels.
/// </summary>
public interface IImagePathResolver
{
    /// <summary>
    /// Returns a full path suitable for binding to an <see cref="Image"/> control for a container photo.
    /// If the underlying file is missing, a fallback path is returned.
    /// </summary>
    string GetContainerPhotoPath(ImageItem photo);

    /// <summary>
    /// Returns a full path suitable for binding to an <see cref="Image"/> control for an item photo.
    /// If the underlying file is missing, a fallback path is returned.
    /// </summary>
    string GetItemPhotoPath(ImageItem photo);

    /// <summary>
    /// Returns the fallback image path used when an entity has no photos.
    /// </summary>
    string GetFallbackImagePath();
}
