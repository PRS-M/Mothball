using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Contracts.Containers;

public sealed record ContainerDetailsResult(Container Container, int TotalItemCount);