using CoreApp.Entities.ContainerAggregate;
using CoreApp.Features.Photos;
using CoreApp.Interfaces;

namespace CoreApp.Features.Containers.Commands;

public sealed class CreateContainerCommandHandler : ICreateContainerCommandHandler
{
    private readonly IInventoryCommandRepository inventoryCommands;
    private readonly ImageService imageService;

    public CreateContainerCommandHandler(
        IInventoryCommandRepository inventoryCommands,
        ImageService imageService)
    {
        this.inventoryCommands = inventoryCommands ?? throw new ArgumentNullException(nameof(inventoryCommands));
        this.imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    public async Task<Container> CreateAsync(string name, string notes, byte[]? photoBytes = null)
    {
        var container = new Container(
            containerId: Guid.NewGuid(),
            name: name,
            notes: notes);

        await inventoryCommands.InsertContainerAsync(container);

        if (photoBytes is { Length: > 0 })
        {
            await imageService.SaveContainerPhotoAsync(container, photoBytes);
        }

        return container;
    }
}
