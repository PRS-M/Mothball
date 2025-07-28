using System;
using CoreApp.Services.Interfaces;
using CoreApp.Utilities;

namespace CoreApp.Services.Implementations;

public class ContainerJsonHandler
{
    private readonly JsonHandler jsonHandler;

    public ContainerJsonHandler(JsonHandler jsonHandler)
    {
        this.jsonHandler = jsonHandler;
    }

    public async Task SaveContainerAsync(Container container)
    {
        if (container == null)
            throw new ArgumentNullException(nameof(container));

        await jsonHandler.SerializeToFile($"{container.Name}.json", Constants.PathToContainers, container);
    }

    public async Task<List<Container>> LoadContainersAsync()
    {
        List<Container> containers = [];
        IEnumerable<string> files = Directory.EnumerateFiles(Constants.PathToContainers);
        foreach (var containerName in files)
        {
            var container = await jsonHandler.DeserializeFromFile<Container>(containerName, Constants.PathToContainers);
            containers.Add(container);
        }

        return containers;
    }
}
