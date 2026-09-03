using CoreApp.Application.Features.Sync;

namespace Mothball.Tests.Unit.Core.Features.Sync;

[TestFixture]
public sealed class SyncOrchestratorTests
{
    [Test]
    public async Task Synchronize_AcknowledgesPushesAndPersistsCursorAfterPull()
    {
        var store = new InMemorySyncOperationStore();
        var workspaceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await store.EnqueueAsync(new PendingSyncOperation(operationId, workspaceId, Guid.NewGuid(), "Item", Guid.NewGuid(), "Update", 1, "{}", null, DateTimeOffset.UtcNow));
        await store.SaveSyncStateAsync(new WorkspaceSyncState(workspaceId, Guid.NewGuid(), null, null, "Pending", false));
        var client = new TestSyncClient(operationId);

        await new SyncOrchestrator(store, client).SynchronizeAsync(workspaceId);

        var pendingOperations = await store.GetPendingAsync(workspaceId, 10);
        var savedState = await store.GetSyncStateAsync(workspaceId);
        Assert.Multiple(() =>
        {
            Assert.That(pendingOperations, Is.Empty);
            Assert.That(savedState!.LastServerCursor, Is.EqualTo("cursor-1"));
            Assert.That(client.PullCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Synchronize_MarksBootstrapRequiredWithoutAdvancingCursor()
    {
        var store = new InMemorySyncOperationStore();
        var workspaceId = Guid.NewGuid();
        await store.SaveSyncStateAsync(new WorkspaceSyncState(workspaceId, Guid.NewGuid(), "old", null, "Ready", false));
        var client = new TestSyncClient(Guid.Empty) { ReturnBootstrapRequired = true };

        await new SyncOrchestrator(store, client).SynchronizeAsync(workspaceId);

        var state = await store.GetSyncStateAsync(workspaceId);
        Assert.Multiple(() =>
        {
            Assert.That(state!.BootstrapRequired, Is.True);
            Assert.That(state.LastServerCursor, Is.EqualTo("old"));
        });
    }

    private sealed class TestSyncClient(Guid acknowledgedOperationId) : ISyncClient
    {
        public bool ReturnBootstrapRequired { get; set; }
        public int PullCount { get; private set; }
        public Task<SyncBootstrapResult> BootstrapAsync(Guid workspaceId, CancellationToken cancellationToken = default)
            => Task.FromResult(new SyncBootstrapResult("snapshot", 1, "{}", "bootstrapped"));

        public Task<SyncPushResult> PushAsync(Guid workspaceId, IReadOnlyList<PendingSyncOperation> operations, CancellationToken cancellationToken = default)
            => Task.FromResult(new SyncPushResult(acknowledgedOperationId == Guid.Empty ? [] : [acknowledgedOperationId], []));

        public Task<SyncChangePage> PullAsync(Guid workspaceId, string? cursor, int pageSize, CancellationToken cancellationToken = default)
        {
            PullCount++;
            return Task.FromResult(ReturnBootstrapRequired
                ? new SyncChangePage([], cursor, false, true)
                : new SyncChangePage([], "cursor-1", false));
        }
    }
}
