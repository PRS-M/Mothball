namespace CoreApp.Application.Contracts.Containers;

public sealed record ContainerDetailsQuantityUpdate(
    ContainerDetailsSummary Summary,
    bool Removed,
    int TotalQuantity,
    int AssignedQuantity,
    int UnassignedQuantity);