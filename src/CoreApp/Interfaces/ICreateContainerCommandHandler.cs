using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Interfaces;

public interface ICreateContainerCommandHandler
{
    Task<Container> CreateAsync(string name, string notes, byte[]? photoBytes = null);
}
