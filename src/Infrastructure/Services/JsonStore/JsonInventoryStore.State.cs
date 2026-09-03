using Infrastructure.Services.JsonStore.Models;
using CoreApp.Application.Features.Sync;

namespace Infrastructure.Services.JsonStore;

public sealed partial class JsonInventoryStore
{
    public sealed class StoreState
    {
        public JsonStoreMetadata Metadata { get; set; } = new();
        public List<JsonContainerRow> Containers { get; set; } = [];
        public List<JsonItemRow> Items { get; set; } = [];
        public List<JsonInventoryRow> Inventories { get; set; } = [];
        public List<JsonImageRow> Images { get; set; } = [];
        public List<JsonRelationRow> Relations { get; set; } = [];
        public List<JsonWorkspaceRow> Workspaces { get; set; } = [];
        public List<PendingSyncOperation> PendingSyncOperations { get; set; } = [];
        public List<EntityTombstone> EntityTombstones { get; set; } = [];
        public List<WorkspaceSyncState> WorkspaceSyncStates { get; set; } = [];
        public List<AppliedRemoteOperation> AppliedRemoteOperations { get; set; } = [];
    }
}
