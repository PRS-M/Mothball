using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Features.Containers.Commands;

public interface IUpdateContainerNotesCommandHandler
{
    Task UpdateAsync(Container container, string notes);
}
