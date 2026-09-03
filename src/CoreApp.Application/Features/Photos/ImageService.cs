using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Utilities;
namespace CoreApp.Application.Features.Photos;

/// <summary>
/// Provides high-level operations for capturing, saving, and persisting photos
/// associated with containers and items.
/// </summary>
public class ImageService
{
    public sealed record TemporaryPhotoCapture(byte[] Bytes, string FileName, string FolderPath, string FullPath);

    private readonly IPhotoSourceReader photoSourceReader;
    private readonly IPhotoFilePersistenceService photoFilePersistence;
    private readonly ITemporaryPhotoService temporaryPhotos;
    private readonly IPhotoDeletionService photoDeletion;
    private readonly IInventoryCommandRepository inventoryRepository;

    public ImageService(
        IPhotoSourceReader photoSourceReader,
        IPhotoFilePersistenceService photoFilePersistence,
        ITemporaryPhotoService temporaryPhotos,
        IPhotoDeletionService photoDeletion,
        IInventoryCommandRepository inventoryRepository)
    {
        this.photoSourceReader = photoSourceReader ?? throw new ArgumentNullException(nameof(photoSourceReader));
        this.photoFilePersistence = photoFilePersistence ?? throw new ArgumentNullException(nameof(photoFilePersistence));
        this.temporaryPhotos = temporaryPhotos ?? throw new ArgumentNullException(nameof(temporaryPhotos));
        this.photoDeletion = photoDeletion ?? throw new ArgumentNullException(nameof(photoDeletion));
        this.inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
    }

    /// <summary>
    /// Captures a new photo for the specified container and persists both the file and metadata.
    /// </summary>
    /// <param name="container">The container to associate the photo with.</param>
    /// <param name="resizeProgress">Optionally receives progress while the photo is resized.</param>
    /// <param name="source">The source from which to capture the photo.</param>
    /// <returns>
    /// A task returning the number of bytes captured and saved; returns 0 if the capture was canceled.
    /// </returns>
    public async Task<int> CaptureContainerPhotoAsync(
        Container container,
        IProgress<double>? resizeProgress = null,
        PhotoSource source = PhotoSource.Library)
    {
        ArgumentNullException.ThrowIfNull(container);
        return await CaptureAndPersistPhotoAsync(
            addImageItem: container.AddImageItem,
            removeImageItem: container.RemoveImageItem,
            saveDirectory: Constants.PathToContainerPhotos,
            resizeProgress: resizeProgress,
            source: source,
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
    /// <param name="resizeProgress">Optionally receives progress while the photo is resized.</param>
    /// <param name="source">The source from which to capture the photo.</param>
    /// <returns>
    /// A task returning the number of bytes captured and saved; returns 0 if the capture was canceled.
    /// </returns>
    public async Task<int> CaptureItemPhotoAsync(
        Item item,
        IProgress<double>? resizeProgress = null,
        PhotoSource source = PhotoSource.Library)
    {
        ArgumentNullException.ThrowIfNull(item);
        return await CaptureAndPersistPhotoAsync(
            addImageItem: item.AddImageItem,
            removeImageItem: item.RemoveImageItem,
            saveDirectory: Constants.PathToItemPhotos,
            resizeProgress: resizeProgress,
            source: source,
            persistAsync: async image =>
            {
                await inventoryRepository.InsertImageItemAsync(image, item.ItemId);
                await inventoryRepository.UpdateItemAsync(item);
            });
    }

    /// <summary>
    /// Captures a photo and stores it in temporary app storage until the owning entity is saved.
    /// </summary>
    /// <param name="resizeProgress">Optionally receives progress while the photo is resized.</param>
    /// <param name="source">The source from which to capture the photo.</param>
    /// <returns>
    /// A temporary capture descriptor containing bytes and file path, or <see langword="null"/> when capture is canceled.
    /// </returns>
    public Task<TemporaryPhotoCapture?> CaptureTemporaryPhotoAsync(
        IProgress<double>? resizeProgress = null,
        PhotoSource source = PhotoSource.Library)
        => temporaryPhotos.CaptureTemporaryPhotoAsync(resizeProgress, source);

    /// <summary>
    /// Deletes a temporary photo if it exists.
    /// </summary>
    /// <param name="fileName">The temporary photo file name to delete.</param>
    public Task DeleteTemporaryPhotoAsync(string fileName)
        => temporaryPhotos.DeleteTemporaryPhotoAsync(fileName);

    /// <summary>
    /// Persists previously captured photo bytes for a container.
    /// </summary>
    /// <param name="container">The container to associate with the photo.</param>
    /// <param name="bytes">The encoded photo bytes to persist.</param>
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
    /// <param name="item">The item to associate with the photo.</param>
    /// <param name="bytes">The encoded photo bytes to persist.</param>
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
    /// <param name="container">The container that owns the photo.</param>
    /// <param name="imageId">The identifier of the photo to delete.</param>
    /// <returns><c>true</c> when the photo was found and deleted; otherwise <c>false</c>.</returns>
    public Task<bool> DeleteContainerPhotoAsync(Container container, Guid imageId)
        => photoDeletion.DeleteContainerPhotoAsync(container, imageId);

    /// <summary>
    /// Deletes a photo for the specified item and removes persisted metadata.
    /// </summary>
    /// <param name="item">The item that owns the photo.</param>
    /// <param name="imageId">The identifier of the photo to delete.</param>
    /// <returns><c>true</c> when the photo was found and deleted; otherwise <c>false</c>.</returns>
    public Task<bool> DeleteItemPhotoAsync(Item item, Guid imageId)
        => photoDeletion.DeleteItemPhotoAsync(item, imageId);

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
        PhotoSource source,
        Func<ImageItem, Task> persistAsync)
    {
        ArgumentNullException.ThrowIfNull(addImageItem);
        ArgumentNullException.ThrowIfNull(removeImageItem);
        ArgumentNullException.ThrowIfNull(saveDirectory);
        ArgumentNullException.ThrowIfNull(persistAsync);

        byte[] bytes = await photoSourceReader.GetPhotoBytesAsync(source, resizeProgress);
        return await PersistPhotoBytesAsync(bytes, addImageItem, removeImageItem, saveDirectory, persistAsync);
    }

    private Task<int> PersistPhotoBytesAsync(
        byte[] bytes,
        Func<ImageItem> addImageItem,
        Action<Guid> removeImageItem,
        string saveDirectory,
        Func<ImageItem, Task> persistAsync)
        => photoFilePersistence.PersistPhotoBytesAsync(
            bytes,
            addImageItem,
            removeImageItem,
            saveDirectory,
            persistAsync);
}
