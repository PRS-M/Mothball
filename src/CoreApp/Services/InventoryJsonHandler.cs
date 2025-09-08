using System;
using CoreApp.Utilities;
using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Services;

public class InventoryJsonHandler
{
    private readonly JsonHandler _jsonHandler;

    public InventoryJsonHandler(JsonHandler jsonHandler)
    {
        _jsonHandler = jsonHandler ?? throw new ArgumentNullException(nameof(jsonHandler));
    }

    public async Task<List<Container>> LoadAsync()
    {
        return await _jsonHandler.DeserializeFromFile<List<Container>>(Constants.InventoryFileName, Constants.PathToData);
    }

    public async Task SaveAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        await _jsonHandler.SerializeToFile(Constants.InventoryFileName, Constants.PathToData, container);
    }
}
