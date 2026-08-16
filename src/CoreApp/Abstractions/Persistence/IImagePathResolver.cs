using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;

namespace CoreApp.Abstractions.Persistence;

/// <summary>
/// Provides UI-friendly file system paths for image value objects while hiding
/// infrastructure concerns (app data root, folder names, etc.) from ViewModels.
/// </summary>
public interface IImagePathResolver
{
    // Container helpers
    /// <param name="container">The value used by the operation.</param>
    string GetPrimaryContainerPhotoPath(Container container);
    /// <param name="container">The value used by the operation.</param>
    IEnumerable<string> GetContainerPhotoPaths(Container container);

    // Item helpers
    /// <param name="item">The value used by the operation.</param>
    string GetPrimaryItemPhotoPath(Item item);
    /// <param name="item">The value used by the operation.</param>
    IEnumerable<string> GetItemPhotoPaths(Item item);

    // Fallback
    string GetFallbackImagePath();
}
