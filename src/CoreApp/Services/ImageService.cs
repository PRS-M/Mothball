using System;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using Microsoft.Extensions.Logging;

namespace CoreApp.Services;

/// <summary>
/// Provides high-level operations for capturing, saving, and persisting photos
/// associated with containers and items.
/// </summary>
public class ImageService
{
    public sealed record TemporaryPhotoCapture(byte[] Bytes, string FileName, string FolderPath, string FullPath);

    private readonly ICameraHandler cameraHandler;
    private readonly IInventoryCommandRepository inventoryRepository;
    private readonly IFileHandler fileHandler;
    private readonly ILogger<ImageService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageService"/> class.
    /// </summary>
    /// <param name="cameraHandler">Service used to capture photos from the device camera.</param>
    /// <param name="inventoryRepository">Domain repository for inserting and updating image-related data.</param>
    /// <param name="fileHandler">Service used to persist captured photos to storage.</param>
    public ImageService(
        ICameraHandler cameraHandler,
        IInventoryCommandRepository inventoryRepository,
        IFileHandler fileHandler,
        ILogger<ImageService> logger)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);
        ArgumentNullException.ThrowIfNull(inventoryRepository);
        ArgumentNullException.ThrowIfNull(fileHandler);

        this.cameraHandler = cameraHandler;
        this.inventoryRepository = inventoryRepository;
        this.fileHandler = fileHandler;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Captures a new photo for the specified container and persists both the file and metadata.
    /// </summary>
    /// <param name="container">The container to associate the photo with.</param>
    /// <returns>
    /// A task returning the number of bytes captured and saved; returns 0 if the capture was canceled.
    /// </returns>
    public async Task<int> CaptureContainerPhotoAsync(Container container, IProgress<double>? resizeProgress = null)
    {
        ArgumentNullException.ThrowIfNull(container);
        return await CaptureAndPersistPhotoAsync(
            addImageItem: container.AddImageItem,
            removeImageItem: container.RemoveImageItem,
            saveDirectory: Constants.PathToContainerPhotos,
            resizeProgress: resizeProgress,
            persistAsync: async image =>
            {
                await inventoryRepository.InsertImageItemAsync(image, container.ContainerId);
                await inventoryRepository.UpdateContainerAsync(container);
            });
    }

    /// <summary>
    /// Captures a new photo for the specified item and persists both the file and metadata.
    /// </summary>
    /// <param name="item">The item to associate the photo with.</param>
    /// <returns>
    /// A task returning the number of bytes captured and saved; returns 0 if the capture was canceled.
    /// </returns>
    public async Task<int> CaptureItemPhotoAsync(Item item, IProgress<double>? resizeProgress = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        return await CaptureAndPersistPhotoAsync(
            addImageItem: item.AddImageItem,
            removeImageItem: item.RemoveImageItem,
            saveDirectory: Constants.PathToItemPhotos,
            resizeProgress: resizeProgress,
            persistAsync: async image =>
            {
                await inventoryRepository.InsertImageItemAsync(image, item.ItemId);
                await inventoryRepository.UpdateItemAsync(item);
            });
    }

    /// <summary>
    /// Captures a photo and stores it in temporary app storage until the owning entity is saved.
    /// </summary>
    /// <returns>
    /// A temporary capture descriptor containing bytes and file path, or <see langword="null"/> when capture is canceled.
    /// </returns>
    public async Task<TemporaryPhotoCapture?> CaptureTemporaryPhotoAsync(IProgress<double>? resizeProgress = null)
    {
        byte[] bytes = await cameraHandler.CapturePhotoAsync(resizeProgress);
        if (bytes.Length == 0)
        {
            return null;
        }

        string tempFileName = $"temp-{Guid.NewGuid():N}.jpg";
        string fullPath = await fileHandler.SaveFileAsync(tempFileName, Constants.PathToTemporaryPhotos, bytes);
        return new TemporaryPhotoCapture(bytes, tempFileName, Constants.PathToTemporaryPhotos, fullPath);
    }

    /// <summary>
    /// Deletes a temporary photo if it exists.
    /// </summary>
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
            // Best-effort cleanup only.
        }
    }

    /// <summary>
    /// Persists previously captured photo bytes for a container.
    /// </summary>
    public async Task<int> SaveContainerPhotoAsync(Container container, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(bytes);

        return await PersistPhotoBytesAsync(
            bytes,
            addImageItem: container.AddImageItem,
            removeImageItem: container.RemoveImageItem,
            saveDirectory: Constants.PathToContainerPhotos,
            persistAsync: async image =>
            {
                await inventoryRepository.InsertImageItemAsync(image, container.ContainerId);
                await inventoryRepository.UpdateContainerAsync(container);
            });
    }

    /// <summary>
    /// Persists previously captured photo bytes for an item.
    /// </summary>
    public async Task<int> SaveItemPhotoAsync(Item item, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(bytes);

        return await PersistPhotoBytesAsync(
            bytes,
            addImageItem: item.AddImageItem,
            removeImageItem: item.RemoveImageItem,
            saveDirectory: Constants.PathToItemPhotos,
            persistAsync: async image =>
            {
                await inventoryRepository.InsertImageItemAsync(image, item.ItemId);
                await inventoryRepository.UpdateItemAsync(item);
            });
    }

    /// <summary>
    /// Deletes a photo for the specified container and removes persisted metadata.
    /// </summary>
    /// <returns><c>true</c> when the photo was found and deleted; otherwise <c>false</c>.</returns>
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

    /// <summary>
    /// Deletes a photo for the specified item and removes persisted metadata.
    /// </summary>
    /// <returns><c>true</c> when the photo was found and deleted; otherwise <c>false</c>.</returns>
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

    /// <summary>
    /// Captures a photo, saves the file, and persists metadata using the provided delegates.
    /// </summary>
    /// <param name="addImageItem">Factory to add and return an <see cref="ImageItem"/> to the owning aggregate.</param>
    /// <param name="removeImageItem">Action to remove a previously added <see cref="ImageItem"/> by its identifier if an error occurs.</param>
    /// <param name="saveDirectory">Target directory where the photo file will be saved.</param>
    /// <param name="persistAsync">Delegate that persists the <see cref="ImageItem"/> and any additional owner updates.</param>
    /// <remarks>
    /// If the camera returns no bytes, the method returns without side effects. If file save fails,
    /// the newly added image is removed to keep the aggregate consistent, and the exception is rethrown.
    /// </remarks>
    /// <returns>
    /// A task returning the number of bytes captured and saved; returns 0 if the capture was canceled.
    /// </returns>
    private async Task<int> CaptureAndPersistPhotoAsync(
        Func<ImageItem> addImageItem,
        Action<Guid> removeImageItem,
        string saveDirectory,
        IProgress<double>? resizeProgress,
        Func<ImageItem, Task> persistAsync)
    {
        ArgumentNullException.ThrowIfNull(addImageItem);
        ArgumentNullException.ThrowIfNull(removeImageItem);
        ArgumentNullException.ThrowIfNull(saveDirectory);
        ArgumentNullException.ThrowIfNull(persistAsync);

        byte[] bytes = await cameraHandler.CapturePhotoAsync(resizeProgress);
        return await PersistPhotoBytesAsync(bytes, addImageItem, removeImageItem, saveDirectory, persistAsync);
    }

    private async Task<int> PersistPhotoBytesAsync(
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
                // Best-effort cleanup only; preserve the original persistence error.
            }

            throw;
        }

        return bytes.Length;
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
            // Best-effort cleanup only.
        }
    }
}
