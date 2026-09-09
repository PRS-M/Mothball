using CoreApp.Domain.Entities.InventoryAggregate;

namespace CoreApp.Domain.Events;

/// <summary>Indicates that an item was created.</summary>
public sealed record ItemCreated(Guid ItemId, string Name) : DomainEventBase;

/// <summary>Indicates that an item's descriptive details changed.</summary>
public sealed record ItemDetailsUpdated(
    Guid ItemId,
    string PreviousName,
    string Name,
    string PreviousDescription,
    string Description) : DomainEventBase;

/// <summary>Indicates that an item's barcode changed.</summary>
public sealed record ItemBarcodeChanged(Guid ItemId, string? PreviousBarcode, string? Barcode) : DomainEventBase;

/// <summary>Indicates that an item was deleted.</summary>
public sealed record ItemDeleted(Guid ItemId) : DomainEventBase;

/// <summary>Indicates that a container was created.</summary>
public sealed record ContainerCreated(Guid ContainerId, string Name) : DomainEventBase;

/// <summary>Indicates that a container's descriptive details changed.</summary>
public sealed record ContainerDetailsUpdated(
    Guid ContainerId,
    string PreviousName,
    string Name,
    string PreviousNotes,
    string Notes) : DomainEventBase;

/// <summary>Indicates that a container's barcode changed.</summary>
public sealed record ContainerBarcodeChanged(Guid ContainerId, string? PreviousBarcode, string? Barcode) : DomainEventBase;

/// <summary>Indicates that a container was deleted.</summary>
public sealed record ContainerDeleted(Guid ContainerId) : DomainEventBase;

/// <summary>Indicates that an item's inventory changed.</summary>
public sealed record InventoryChanged(
    Guid ItemId,
    int PreviousTotalQuantity,
    int TotalQuantity,
    int PreviousAssignedQuantity,
    int AssignedQuantity,
    IReadOnlyCollection<Guid> AffectedContainerIds) : DomainEventBase;

/// <summary>Indicates that inventory was withdrawn from an item.</summary>
public sealed record InventoryWithdrawn(
    Guid ItemId,
    int PreviousTotalQuantity,
    int TotalQuantity,
    IReadOnlyCollection<ItemContainerAllocation> RemainingAllocations) : DomainEventBase;

/// <summary>Indicates that an item reached zero inventory and was removed.</summary>
public sealed record ItemExhausted(Guid ItemId) : DomainEventBase;

/// <summary>Indicates that a restore operation committed successfully.</summary>
public sealed record InventoryRestored(
    int CreatedItems,
    int UpdatedItems,
    int DeletedItems,
    int CreatedContainers,
    int UpdatedContainers,
    int DeletedContainers) : DomainEventBase;

/// <summary>Indicates that a tag was renamed.</summary>
public sealed record TagRenamed(Guid TagId, string PreviousName, string Name) : DomainEventBase;
