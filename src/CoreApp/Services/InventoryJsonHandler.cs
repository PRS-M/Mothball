using System;
using CoreApp.Utilities;
using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Services;

public class InventoryJsonHandler
{
    private readonly JsonHandler jsonHandler;

    public InventoryJsonHandler(JsonHandler jsonHandler)
    {
        this.jsonHandler = jsonHandler ?? throw new ArgumentNullException(nameof(jsonHandler));
    }

    public async Task<List<Container>> LoadAsync()
    {
        return await jsonHandler.DeserializeFromFile<List<Container>>(Constants.InventoryFileName, Constants.PathToData);
    }

    public Task SaveAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return SaveAsync([container]);
    }

    public Task SaveAsync(List<Container> containers)
    {
        ArgumentNullException.ThrowIfNull(containers);
        return jsonHandler.SerializeToFile(Constants.InventoryFileName, Constants.PathToData, containers);
    }
}
