using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Containers.Commands;

public interface ICreateContainerCommandHandler
{
    Task<Container> CreateAsync(string name, string notes, byte[]? photoBytes = null);
}
