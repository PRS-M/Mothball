using System;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;

namespace CoreApp.Services;

public class ImageService
{
    private readonly ICameraHandler cameraHandler;
    private readonly IInventoryDomainRepository inventoryRepository;

    public ImageService(ICameraHandler cameraHandler, IInventoryDomainRepository inventoryRepository)
    {
        ArgumentNullException.ThrowIfNull(cameraHandler);
        ArgumentNullException.ThrowIfNull(inventoryRepository);

        this.cameraHandler = cameraHandler;
        this.inventoryRepository = inventoryRepository;
    }

    public async Task CaptureContainerPhotoAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        ImageItem capturedPhoto = await cameraHandler.CaptureContainerPhotoAsync(container);
        container.Photos.Add(capturedPhoto);

        await inventoryRepository.InsertImageItem(capturedPhoto);
        await inventoryRepository.UpdateContainerAsync(container);
    }

    public async Task CaptureItemPhotoAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ImageItem capturedPhoto = await cameraHandler.CaptureItemPhotoAsync(item);
        item.Photos.Add(capturedPhoto);

        await inventoryRepository.InsertImageItem(capturedPhoto);
        await inventoryRepository.UpdateItemAsync(item);
    }
}
