using System;
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
}
