using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Utilities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Images;

/// <summary>
/// Translates image value objects into concrete file-system paths. Centralizes
/// path construction and fallback logic to keep ViewModels free of IO concerns.
/// </summary>
public sealed class ImagePathResolver : IImagePathResolver
{
    private readonly IFileHandler fileHandler;
    private readonly ILogger<ImagePathResolver> logger;

    public ImagePathResolver(IFileHandler fileHandler, ILogger<ImagePathResolver> logger)
    {
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string GetPrimaryContainerPhotoPath(Container container)
        => FirstOrFallback(container.Photos, Constants.PathToContainerPhotos);

    /// <inheritdoc />
    public IEnumerable<string> GetContainerPhotoPaths(Container container)
        => PathsOrFallback(container.Photos, Constants.PathToContainerPhotos);

    /// <inheritdoc />
    public string GetPrimaryItemPhotoPath(Item item)
        => FirstOrFallback(item.Photos, Constants.PathToItemPhotos);

    /// <inheritdoc />
    public IEnumerable<string> GetItemPhotoPaths(Item item)
        => PathsOrFallback(item.Photos, Constants.PathToItemPhotos);

    /// <inheritdoc />
    public string GetFallbackImagePath() => "mothball_logo.png"; // central fallback

    private string FirstOrFallback(IEnumerable<ImageItem> photos, string folder)
        => photos.Any() ? BuildPath(folder, photos.First().FileName) : GetFallbackImagePath();

    private IEnumerable<string> PathsOrFallback(IEnumerable<ImageItem> photos, string folder)
    {
        if (photos.Any())
            foreach (var p in photos)
                yield return BuildPath(folder, p.FileName);
        else
            yield return GetFallbackImagePath();
    }

    private string BuildPath(string folder, string fileName)
    {
        try
        {
            var root = fileHandler.AppDataPath;
            return Path.Combine(root, folder, fileName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to build image path for file {FileName} in {Folder}; using fallback image.", fileName, folder);
            return GetFallbackImagePath();
        }
    }
}
