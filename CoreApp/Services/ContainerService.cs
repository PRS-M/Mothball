using System;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;

namespace CoreApp.Services;

public class ContainerService
{
    private readonly ICameraHandler cameraHandler;
    private readonly IInventoryDomainRepository inventoryRepository;

    public ContainerService(ICameraHandler cameraHandler, IInventoryDomainRepository inventoryRepository)
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
        await inventoryRepository.UpdateContainerAsync(container);
    }
}
