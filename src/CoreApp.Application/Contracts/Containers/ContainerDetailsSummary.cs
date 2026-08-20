using CoreApp.Domain.Entities.ContainerAggregate;

namespace CoreApp.Application.Contracts.Containers;

public sealed record ContainerDetailsSummary(Container Container, int ItemTypesCount, int TotalItemCount);