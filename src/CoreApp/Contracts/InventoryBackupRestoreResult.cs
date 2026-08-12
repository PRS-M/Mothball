namespace CoreApp.Contracts;

public sealed record InventoryBackupRestoreResult
{
    public int AddedContainers { get; init; }
    public int AddedItems { get; init; }
    public int AddedRelations { get; init; }
    public int AddedRelationQuantity { get; init; }
    public int AddedImages { get; init; }

    public int UpdatedContainers { get; init; }
    public int UpdatedItems { get; init; }

    public int DeletedContainers { get; init; }
    public int DeletedItems { get; init; }
    public int DeletedRelations { get; init; }
    public int DeletedImages { get; init; }

    public int SkippedExistingContainers { get; init; }
    public int SkippedExistingItems { get; init; }
    public int SkippedExistingRelations { get; init; }
    public int SkippedExistingImages { get; init; }

    public int SkippedInvalidRelations { get; init; }
    public int SkippedImagesWithMissingOwner { get; init; }
}
