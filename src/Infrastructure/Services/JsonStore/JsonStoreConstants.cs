using CoreApp.Application.Utilities;

namespace Infrastructure.Services.JsonStore;

internal static class JsonStoreConstants
{
    public static readonly string StoreRoot = Path.Combine(Constants.PathToData, "OperationalStore");

    public static readonly string SlotA = Path.Combine(StoreRoot, "slotA");
    public static readonly string SlotB = Path.Combine(StoreRoot, "slotB");

    public static readonly string ManifestAFileName = "manifestA.json";
    public static readonly string ManifestBFileName = "manifestB.json";

    public static readonly string MetadataFileName = "metadata.json";
    public static readonly string CommitInfoFileName = "commit.json";

    public static readonly string ContainersFileName = "containers.json";
    public static readonly string ItemsFileName = "items.json";
    public static readonly string InventoriesFileName = "inventories.json";
    public static readonly string ImagesFileName = "images.json";
    public static readonly string RelationsFileName = "relations.json";
    public static readonly string WorkspacesFileName = "workspaces.json";
    public static readonly string PendingSyncOperationsFileName = "pendingSyncOperations.json";
    public static readonly string EntityTombstonesFileName = "entityTombstones.json";
    public static readonly string WorkspaceSyncStatesFileName = "workspaceSyncStates.json";
    public static readonly string AppliedRemoteOperationsFileName = "appliedRemoteOperations.json";
    public static readonly string CanonicalBalancesFileName = "canonicalBalances.json";
    public static readonly string CanonicalMovementsFileName = "canonicalMovements.json";

    public static readonly string[] ExpectedFiles =
    [
        MetadataFileName,
        CommitInfoFileName,
        ContainersFileName,
        ItemsFileName,
        InventoriesFileName,
        ImagesFileName,
        RelationsFileName,
    ];

    public static string SlotFolder(string slot) =>
        slot.Equals("B", StringComparison.OrdinalIgnoreCase) ? SlotB : SlotA;

    public static string OtherSlot(string slot) =>
        slot.Equals("B", StringComparison.OrdinalIgnoreCase) ? "A" : "B";
}
