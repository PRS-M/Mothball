using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Contracts.Containers;

public sealed record ContainerDetailsSummary(Container Container, int ItemTypesCount, int TotalItemCount);