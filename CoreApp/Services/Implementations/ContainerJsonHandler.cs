using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CoreApp.Services.Interfaces;
using CoreApp.Utilities;
using CoreApp;

namespace CoreApp.Services.Implementations
{
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
            List<Container> containers = new List<Container>();
            IEnumerable<string> files = Directory.EnumerateFiles(Constants.PathToContainers);
            foreach (var containerName in files)
            {
                var container = await jsonHandler.DeserializeFromFile<Container>(containerName, Constants.PathToContainers);
                containers.Add(container);
            }

            return containers;
        }

        public async Task<Container> LoadContainerFromFileAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            return await jsonHandler.DeserializeFromFile<Container>(fileName, Constants.PathToContainers);
        }
    }
}
