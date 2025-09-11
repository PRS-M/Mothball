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

    public string GetContainerPhotoPath(ImageItem photo)
        => BuildPath(Constants.PathToContainerPhotos, photo.FileName);

    public string GetItemPhotoPath(ImageItem photo)
        => BuildPath(Constants.PathToItemPhotos, photo.FileName);

    public string GetFallbackImagePath() => "dotnet_bot.png"; // central fallback

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
