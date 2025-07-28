using System;
using CoreApp.Services.Interfaces;
using CoreApp.Utilities;

namespace CoreApp.Services.Implementations;

public class ContainerJsonHandler
{
    private readonly JsonHandler jsonHandler;
    private readonly IFileHandler fileHandler;

    public ContainerJsonHandler(JsonHandler jsonHandler, IFileHandler fileHandler)
    {
        this.jsonHandler = jsonHandler;
        this.fileHandler = fileHandler;
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
            var container = await JsonHandler.DeserializeFromFile<Container>(containerName, Constants.PathToContainers, fileHandler.GetAppDataPath());
            containers.Add(container);
        }

        return containers;
    }
}
