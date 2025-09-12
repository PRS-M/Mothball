using System;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace CoreApp.Services;

public class ImageService
{
    private readonly ICameraHandler cameraHandler;
    private readonly IInventoryDomainRepository inventoryRepository;
    private readonly IFileHandler fileHandler;

    public ImageService(ICameraHandler cameraHandler, IInventoryDomainRepository inventoryRepository, IFileHandler fileHandler)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);
        ArgumentNullException.ThrowIfNull(inventoryRepository);
        ArgumentNullException.ThrowIfNull(fileHandler);

        this.cameraHandler = cameraHandler;
        this.inventoryRepository = inventoryRepository;
        this.fileHandler = fileHandler;
    }

    public async Task CaptureContainerPhotoAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        byte[] bytes = await cameraHandler.CapturePhotoAsync();
        if (bytes.Length == 0) return;

        ImageItem image = container.AddImageItem();
        try
        {
            await fileHandler.SaveFileAsync(image.FileName, Constants.PathToContainerPhotos, bytes);
        }
        catch
        {
            container.RemoveImageItem(image.ImageId);
            throw;
        }

        await inventoryRepository.InsertImageItemAsync(image, container.ContainerId);
        await inventoryRepository.UpdateContainerAsync(container);
    }

    public async Task CaptureItemPhotoAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        byte[] bytes = await cameraHandler.CapturePhotoAsync();
        if (bytes.Length == 0) return;

        ImageItem image = item.AddImageItem();
        try
        {
            await fileHandler.SaveFileAsync(image.FileName, Constants.PathToItemPhotos, bytes);
        }
        catch
        {
            item.RemoveImageItem(image.ImageId);
            throw;
        }

        await inventoryRepository.InsertImageItemAsync(image, item.ItemId);
        await inventoryRepository.UpdateItemAsync(item);
    }
}
