using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Contracts;

public sealed record ContainerDetailsResult(Container Container, int TotalItemCount);
