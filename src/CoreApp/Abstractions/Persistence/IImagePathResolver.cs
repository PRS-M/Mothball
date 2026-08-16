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
    string GetPrimaryContainerPhotoPath(Container container);
    IEnumerable<string> GetContainerPhotoPaths(Container container);

    // Item helpers
    string GetPrimaryItemPhotoPath(Item item);
    IEnumerable<string> GetItemPhotoPaths(Item item);

    // Fallback
    string GetFallbackImagePath();
}
