using System;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace CoreApp.Services;

/// <summary>
/// Provides high-level operations for capturing, saving, and persisting photos
/// associated with containers and items.
/// </summary>
public class ImageService
{
    private readonly ICameraHandler cameraHandler;
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly IFileHandler fileHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageService"/> class.
    /// </summary>
    /// <param name="cameraHandler">Service used to capture photos from the device camera.</param>
    /// <param name="inventoryRepository">Domain repository for inserting and updating image-related data.</param>
    /// <param name="fileHandler">Service used to persist captured photos to storage.</param>
    public ImageService(ICameraHandler cameraHandler, IInventoryDomainRepository inventoryRepository, IFileHandler fileHandler)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);
        ArgumentNullException.ThrowIfNull(inventoryRepository);
        ArgumentNullException.ThrowIfNull(fileHandler);

        this.cameraHandler = cameraHandler;
        this.inventoryRepository = inventoryRepository;
        this.fileHandler = fileHandler;
    }

    /// <summary>
    /// Captures a new photo for the specified container and persists both the file and metadata.
    /// </summary>
    /// <param name="container">The container to associate the photo with.</param>
    /// <returns>
    /// A task returning the number of bytes captured and saved; returns 0 if the capture was canceled.
    /// </returns>
    public async Task<int> CaptureContainerPhotoAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return await CaptureAndPersistPhotoAsync(
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
    /// Captures a new photo for the specified item and persists both the file and metadata.
    /// </summary>
    /// <param name="item">The item to associate the photo with.</param>
    /// <returns>
    /// A task returning the number of bytes captured and saved; returns 0 if the capture was canceled.
    /// </returns>
    public async Task<int> CaptureItemPhotoAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return await CaptureAndPersistPhotoAsync(
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
        Func<ImageItem, Task> persistAsync)
    {
        ArgumentNullException.ThrowIfNull(addImageItem);
        ArgumentNullException.ThrowIfNull(removeImageItem);
        ArgumentNullException.ThrowIfNull(saveDirectory);
        ArgumentNullException.ThrowIfNull(persistAsync);

        byte[] bytes = await cameraHandler.CapturePhotoAsync();
        if (bytes.Length == 0) return 0;

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

        await persistAsync(image);
        return bytes.Length;
    }
}
