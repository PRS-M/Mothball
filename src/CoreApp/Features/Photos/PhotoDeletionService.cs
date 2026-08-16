using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using Microsoft.Extensions.Logging;

namespace CoreApp.Features.Photos;

public sealed class PhotoDeletionService : IPhotoDeletionService
{
    private readonly IInventoryCommandRepository inventoryRepository;
    private readonly IFileHandler fileHandler;
    private readonly ILogger<PhotoDeletionService> logger;

    public PhotoDeletionService(
        IInventoryCommandRepository inventoryRepository,
        IFileHandler fileHandler,
        ILogger<PhotoDeletionService> logger)
    {
        this.inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> DeleteContainerPhotoAsync(Container container, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(container);

        if (!container.Photos.Any(p => p.ImageId == imageId))
        {
            return false;
        }

        container.RemoveImageItem(imageId);

        try
        {
            await inventoryRepository.DeleteContainerPhotoAsync(container, imageId).ConfigureAwait(false);
        }
        catch
        {
            container.AddImageItem(imageId);
            throw;
        }

        await DeletePhotoFileBestEffortAsync(imageId, Constants.PathToContainerPhotos).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteItemPhotoAsync(Item item, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.Photos.Any(p => p.ImageId == imageId))
        {
            return false;
        }

        item.RemoveImageItem(imageId);

        try
        {
            await inventoryRepository.DeleteItemPhotoAsync(item, imageId).ConfigureAwait(false);
        }
        catch
        {
            item.AddImageItem(imageId);
            throw;
        }

        await DeletePhotoFileBestEffortAsync(imageId, Constants.PathToItemPhotos).ConfigureAwait(false);
        return true;
    }

    public async Task DeleteItemPhotoFilesBestEffortAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        foreach (var photo in item.Photos)
        {
            await DeletePhotoFileBestEffortAsync(photo.ImageId, Constants.PathToItemPhotos).ConfigureAwait(false);
        }
    }

    private async Task DeletePhotoFileBestEffortAsync(Guid imageId, string folderPath)
    {
        try
        {
            await fileHandler.DeleteFileAsync($"{imageId}.jpg", folderPath).ConfigureAwait(false);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogDebug(ex, "Photo file for image {ImageId} was not found in {FolderPath} during cleanup.", imageId, folderPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete photo file for image {ImageId} from {FolderPath}.", imageId, folderPath);
        }
    }
}
