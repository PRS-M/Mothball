using System;
using CoreApp.Contracts;
using CoreApp.Shared.Serialization;
using CoreApp.Utilities;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Shared;

namespace CoreApp.Features.Backup.Serialization;

public class InventoryJsonHandler
{
    private readonly JsonHandler jsonHandler;

    public InventoryJsonHandler(JsonHandler jsonHandler)
    {
        this.jsonHandler = jsonHandler ?? throw new ArgumentNullException(nameof(jsonHandler));
    }

    public async Task<List<Container>> LoadAsync()
    {
        var storedContainers = await jsonHandler.DeserializeFromFile<List<InventoryContainerDto>>(
            Constants.InventoryFileName,
            Constants.PathToData);

        return storedContainers.Select(ToDomain).ToList();
    }

    public Task SaveAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return SaveAsync([container]);
    }

    public Task SaveAsync(List<Container> containers)
    {
        ArgumentNullException.ThrowIfNull(containers);
        return jsonHandler.SerializeToFile(
            Constants.InventoryFileName,
            Constants.PathToData,
            containers.Select(ToDto).ToList());
    }

    private static InventoryContainerDto ToDto(Container container) => new(
        container.ContainerId,
        container.Name,
        container.Notes,
        container.Photos.Select(p => new InventoryImageDto(p.ImageId)).ToList(),
        container.Items.Select(i => new InventoryStoredItemDto(i.ItemId, i.Quantity)).ToList());

    private static Container ToDomain(InventoryContainerDto stored)
    {
        var container = new Container(stored.ContainerId, stored.Name, stored.Notes);
        container.AddImageItems((stored.Photos ?? []).Select(p => new ImageItem(p.ImageId)));

        foreach (var item in stored.Items ?? [])
        {
            container.AddItem(item.ItemId, item.Quantity);
        }

        return container;
    }
}
