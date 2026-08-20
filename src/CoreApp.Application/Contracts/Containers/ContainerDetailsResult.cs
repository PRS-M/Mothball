using CoreApp.Domain.Entities.ContainerAggregate;

namespace CoreApp.Application.Contracts.Containers;

public sealed record ContainerDetailsResult(Container Container, int TotalItemCount);