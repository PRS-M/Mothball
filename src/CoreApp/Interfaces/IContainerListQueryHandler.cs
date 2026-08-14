using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Interfaces;

public interface IContainerListQueryHandler
{
    Task<List<Container>> QueryAsync(bool emptyOnly, string? searchTerm = null, int? pageNumber = null, int? pageSize = null);
}
