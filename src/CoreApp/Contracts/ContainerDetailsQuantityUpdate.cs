namespace CoreApp.Contracts;

public sealed record ContainerDetailsQuantityUpdate(
    ContainerDetailsSummary Summary,
    bool Removed);