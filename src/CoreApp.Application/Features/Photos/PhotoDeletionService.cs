using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Utilities;
using Microsoft.Extensions.Logging;

namespace CoreApp.Application.Features.Photos;

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

    /// <inheritdoc />
    public async Task<bool> DeleteContainerPhotoAsync(Container container, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(container);

        if (!container.Photos.Any(p => p.ImageId == imageId))
        {
            return false;
        }

        var photo = container.Photos.First(p => p.ImageId == imageId);
        container.RemoveImageItem(imageId);

        try
        {
            await inventoryRepository.DeleteContainerPhotoAsync(container, imageId).ConfigureAwait(false);
        }
        catch
        {
            container.AddImageItem(imageId);
            container.ClearDomainEvents();
            throw;
        }

        await DeletePhotoFileBestEffortAsync(photo, Constants.PathToContainerPhotos).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteItemPhotoAsync(Item item, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.Photos.Any(p => p.ImageId == imageId))
        {
            return false;
        }

        var photo = item.Photos.First(p => p.ImageId == imageId);
        item.RemoveImageItem(imageId);

        try
        {
            await inventoryRepository.DeleteItemPhotoAsync(item, imageId).ConfigureAwait(false);
        }
        catch
        {
            item.AddImageItem(imageId);
            item.ClearDomainEvents();
            throw;
        }

        await DeletePhotoFileBestEffortAsync(photo, Constants.PathToItemPhotos).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task DeleteItemPhotoFilesBestEffortAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        foreach (var photo in item.Photos)
        {
            await DeletePhotoFileBestEffortAsync(photo, Constants.PathToItemPhotos).ConfigureAwait(false);
        }
    }

    private async Task DeletePhotoFileBestEffortAsync(ImageItem photo, string folderPath)
    {
        if (photo.IsSharedAsset)
        {
            return;
        }

        try
        {
            await fileHandler.DeleteFileAsync(photo.FileName, folderPath).ConfigureAwait(false);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogDebug(ex, "Photo file for image {ImageId} was not found in {FolderPath} during cleanup.", photo.ImageId, folderPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete photo file for image {ImageId} from {FolderPath}.", photo.ImageId, folderPath);
        }
    }
}
