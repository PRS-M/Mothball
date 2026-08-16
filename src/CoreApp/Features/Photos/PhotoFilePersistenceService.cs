using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreApp.Features.Photos;

public sealed class PhotoFilePersistenceService : IPhotoFilePersistenceService
{
    private readonly IFileHandler fileHandler;
    private readonly ILogger<PhotoFilePersistenceService> logger;

    public PhotoFilePersistenceService(
        IFileHandler fileHandler,
        ILogger<PhotoFilePersistenceService> logger)
    {
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> PersistPhotoBytesAsync(
        byte[] bytes,
        Func<ImageItem> addImageItem,
        Action<Guid> removeImageItem,
        string saveDirectory,
        Func<ImageItem, Task> persistAsync)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(addImageItem);
        ArgumentNullException.ThrowIfNull(removeImageItem);
        ArgumentNullException.ThrowIfNull(saveDirectory);
        ArgumentNullException.ThrowIfNull(persistAsync);

        if (bytes.Length == 0)
        {
            return 0;
        }

        ImageItem image = addImageItem();
        try
        {
            await fileHandler.SaveFileAsync(image.FileName, saveDirectory, bytes);
        }
        catch
        {
            removeImageItem(image.ImageId);
            throw;
        }

        try
        {
            await persistAsync(image);
        }
        catch
        {
            removeImageItem(image.ImageId);

            try
            {
                await fileHandler.DeleteFileAsync(image.FileName, saveDirectory);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete photo file {FileName} from {Directory} after metadata persistence failed.", image.FileName, saveDirectory);
            }

            throw;
        }

        return bytes.Length;
    }
}
