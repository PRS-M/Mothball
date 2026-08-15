using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Interfaces;

public interface IUpdateContainerNotesCommandHandler
{
    Task UpdateAsync(Container container, string notes);
}
