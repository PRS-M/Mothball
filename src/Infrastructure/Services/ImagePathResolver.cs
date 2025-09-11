using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace MothballMobile.Infrastructure;

/// <summary>
/// Translates image value objects into concrete file-system paths. Centralizes
/// path construction and fallback logic to keep ViewModels free of IO concerns.
/// </summary>
public sealed class ImagePathResolver : IImagePathResolver
{
    private readonly IFileHandler _fileHandler;

    public ImagePathResolver(IFileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }

    /// <inheritdoc />
    public string GetPrimaryContainerPhotoPath(Container container)
        => FirstOrFallback(container?.Photos, Constants.PathToContainerPhotos);

    /// <inheritdoc />
    public IEnumerable<string> GetContainerPhotoPaths(Container container)
        => PathsOrFallback(container?.Photos, Constants.PathToContainerPhotos);

    /// <inheritdoc />
    public string GetPrimaryItemPhotoPath(Item item)
        => FirstOrFallback(item?.Photos, Constants.PathToItemPhotos);

    /// <inheritdoc />
    public IEnumerable<string> GetItemPhotoPaths(Item item)
        => PathsOrFallback(item?.Photos, Constants.PathToItemPhotos);

    /// <inheritdoc />
    public string GetFallbackImagePath() => "dotnet_bot.png"; // central fallback

    private string FirstOrFallback(IEnumerable<ImageItem>? photos, string folder)
        => photos != null && photos.Any() ? BuildPath(folder, photos.First().FileName) : GetFallbackImagePath();

    private IEnumerable<string> PathsOrFallback(IEnumerable<ImageItem>? photos, string folder)
    {
        if (photos != null && photos.Any())
            foreach (var p in photos)
                yield return BuildPath(folder, p.FileName);
        else
            yield return GetFallbackImagePath();
    }

    private string BuildPath(string folder, string fileName)
    {
        try
        {
            var root = _fileHandler.GetAppDataPath();
            return System.IO.Path.Combine(root, folder, fileName);
        }
        catch
        {
            return GetFallbackImagePath();
        }
    }
}
