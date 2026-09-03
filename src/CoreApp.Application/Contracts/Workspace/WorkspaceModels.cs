namespace CoreApp.Application.Contracts.Workspace;

/// <summary>Classifies the workspace product experience.</summary>
public enum WorkspaceKind { PersonalStorage, WmsExperimental }

/// <summary>Stable synchronization and authorization boundary for inventory data.</summary>
public sealed record Workspace(Guid WorkspaceId, string Name, WorkspaceKind Kind, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc, long Version = 0);

/// <summary>Default canonical locations and dimensions for a workspace.</summary>
public sealed record WorkspaceDefaults(Guid WorkspaceId, Guid DefaultWarehouseId, Guid UnassignedLocationId, Guid DefaultUnitOfMeasureId, Guid DefaultStockStatusId);

/// <summary>Provides the local workspace context for application operations.</summary>
public interface IWorkspaceContext
{
    Task<(Workspace Workspace, WorkspaceDefaults Defaults)> EnsureDefaultAsync(CancellationToken cancellationToken = default);
}

/// <summary>Creates stable local defaults for legacy Personal Storage data.</summary>
public static class LocalWorkspaceDefaults
{
    public static readonly Guid WorkspaceId = Guid.Parse("5a6b4c3d-2e1f-4039-8a7b-6c5d4e3f2a10");
    public static readonly Guid WarehouseId = Guid.Parse("6b7c5d4e-3f2a-4140-9b8c-7d6e5f4a3b21");
    public static readonly Guid UnassignedLocationId = Guid.Parse("7c8d6e5f-4a3b-4251-ac9d-8e7f6a5b4c32");
    public static readonly Guid EachUnitId = Guid.Parse("8d9e7f6a-5b4c-4362-bdae-9f807b6c5d43");
    public static readonly Guid AvailableStatusId = Guid.Parse("9e8f7a6b-6c5d-4473-cebf-a0918c7d6e54");

    public static (Workspace Workspace, WorkspaceDefaults Defaults) Create(DateTimeOffset now)
    {
        var workspace = new Workspace(WorkspaceId, "Personal Storage", WorkspaceKind.PersonalStorage, now, now);
        var defaults = new WorkspaceDefaults(WorkspaceId, WarehouseId, UnassignedLocationId, EachUnitId, AvailableStatusId);
        return (workspace, defaults);
    }
}
