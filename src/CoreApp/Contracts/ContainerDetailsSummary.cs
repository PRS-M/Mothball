using CoreApp.Entities.ContainerAggregate;

namespace CoreApp.Contracts;

public sealed record ContainerDetailsSummary(
    Container Container,
    int ItemTypesCount,
    int TotalItemCount);