using System;
using CoreApp.Models;
using CoreApp;
using CoreApp.Utilities;

namespace CoreApp.Services.Implementations;

public class InventoryJsonHandler
{
    private readonly JsonHandler _jsonHandler;

    public InventoryJsonHandler(JsonHandler jsonHandler)
    {
        _jsonHandler = jsonHandler ?? throw new ArgumentNullException(nameof(jsonHandler));
    }

    public async Task<InventoryRoot> LoadAsync()
    {
        try
        {
            return await _jsonHandler.DeserializeFromFile<InventoryRoot>(Constants.InventoryFileName, Constants.PathToData);
        }
        catch (FileNotFoundException)
        {
            // No aggregate yet; attempt migration from legacy per-container files.
            var inventory = new InventoryRoot();
            foreach (var fileName in _jsonHandler.EnumerateJsonFiles(Constants.PathToContainers))
            {
                try
                {
                    var container = await _jsonHandler.DeserializeFromFile<Container>(fileName, Constants.PathToContainers);
                    inventory.AddContainer(container);
                }
                catch
                {
                    // Ignore malformed legacy files.
                }
            }

            // Items remain empty during legacy import; future: scan item files if present

            // Persist freshly built inventory
            await SaveAsync(inventory);
            return inventory;
        }
        catch (DirectoryNotFoundException)
        {
            return new InventoryRoot();
        }
    }

    public async Task SaveAsync(InventoryRoot inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        await _jsonHandler.SerializeToFile(Constants.InventoryFileName, Constants.PathToData, inventory);
    }
}
