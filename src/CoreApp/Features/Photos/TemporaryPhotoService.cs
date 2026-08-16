using CoreApp.Utilities;
using Microsoft.Extensions.Logging;

namespace CoreApp.Features.Photos;

public sealed class TemporaryPhotoService : ITemporaryPhotoService
{
    private readonly IPhotoSourceReader photoSourceReader;
    private readonly IFileHandler fileHandler;
    private readonly ILogger<TemporaryPhotoService> logger;

    public TemporaryPhotoService(
        IPhotoSourceReader photoSourceReader,
        IFileHandler fileHandler,
        ILogger<TemporaryPhotoService> logger)
    {
        this.photoSourceReader = photoSourceReader ?? throw new ArgumentNullException(nameof(photoSourceReader));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ImageService.TemporaryPhotoCapture?> CaptureTemporaryPhotoAsync(
        IProgress<double>? resizeProgress = null,
        PhotoSource source = PhotoSource.Library)
    {
        byte[] bytes = await photoSourceReader.GetPhotoBytesAsync(source, resizeProgress);
        if (bytes.Length == 0)
        {
            return null;
        }

        string tempFileName = $"temp-{Guid.NewGuid():N}.jpg";
        string fullPath = await fileHandler.SaveFileAsync(tempFileName, Constants.PathToTemporaryPhotos, bytes);
        return new ImageService.TemporaryPhotoCapture(bytes, tempFileName, Constants.PathToTemporaryPhotos, fullPath);
    }

    public async Task DeleteTemporaryPhotoAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        try
        {
            await fileHandler.DeleteFileAsync(fileName, Constants.PathToTemporaryPhotos);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogDebug(ex, "Temporary photo file {FileName} was not found during cleanup.", fileName);
        }
    }
}
