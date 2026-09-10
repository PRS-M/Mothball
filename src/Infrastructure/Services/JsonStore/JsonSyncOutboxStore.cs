using CoreApp.Application.Abstractions.Sync;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore;

/// <summary>JSON operational-store persistence for the local synchronization outbox.</summary>
public sealed class JsonSyncOutboxStore : ISyncOutboxStore, ISyncDeviceIdentity
{
    private readonly JsonInventoryStore store;

    public JsonSyncOutboxStore(JsonInventoryStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public string DeviceId => initializedDeviceId ??= Guid.NewGuid().ToString("N");

    private string? initializedDeviceId;

    public async Task EnqueueAsync(IReadOnlyCollection<SyncOutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(state =>
        {
            initializedDeviceId = state.Metadata.SyncDeviceId;
            var known = state.SyncOutbox.Select(message => message.EventId).ToHashSet();
            foreach (var message in messages)
            {
                if (!known.Add(message.EventId)) continue;
                state.SyncOutbox.Add(ToRow(message with
                {
                    SourceDeviceId = state.Metadata.SyncDeviceId,
                    Sequence = state.Metadata.NextSyncSequence++,
                }));
            }
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<SyncOutboxMessage>> ClaimPendingAsync(int batchSize, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize));
        List<SyncOutboxMessage> claimed = [];
        await store.UpdateAsync(state =>
        {
            initializedDeviceId = state.Metadata.SyncDeviceId;
            var rows = state.SyncOutbox.Where(row => (row.Status == (int)SyncOutboxStatus.Pending || row.Status == (int)SyncOutboxStatus.Failed) &&
                                                     (row.NextAttemptUtc is null || row.NextAttemptUtc <= nowUtc))
                .OrderBy(row => row.Sequence).Take(batchSize).ToList();
            foreach (var row in rows)
            {
                row.Status = (int)SyncOutboxStatus.InFlight;
                claimed.Add(ToMessage(row));
            }
            return Task.CompletedTask;
        }, cancellationToken);
        return claimed;
    }

    public Task MarkSentAsync(IReadOnlyCollection<Guid> eventIds, CancellationToken cancellationToken = default) =>
        store.UpdateAsync(state =>
        {
            foreach (var row in state.SyncOutbox.Where(row => eventIds.Contains(row.EventId)))
            {
                row.Status = (int)SyncOutboxStatus.Sent;
                row.NextAttemptUtc = null;
                row.LastError = null;
            }
            return Task.CompletedTask;
        }, cancellationToken);

    public Task MarkFailedAsync(IReadOnlyCollection<Guid> eventIds, string error, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) =>
        store.UpdateAsync(state =>
        {
            foreach (var row in state.SyncOutbox.Where(row => eventIds.Contains(row.EventId)))
            {
                row.AttemptCount++;
                row.Status = (int)SyncOutboxStatus.Failed;
                row.NextAttemptUtc = nowUtc.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(row.AttemptCount - 1, 6))));
                row.LastError = error;
            }
            return Task.CompletedTask;
        }, cancellationToken);

    private static JsonSyncOutboxRow ToRow(SyncOutboxMessage message) => new()
    {
        EventId = message.EventId, SourceDeviceId = message.SourceDeviceId, Sequence = message.Sequence,
        EventType = message.EventType, SchemaVersion = message.SchemaVersion, OccurredUtc = message.OccurredUtc,
        AggregateType = message.AggregateType, AggregateId = message.AggregateId, PayloadJson = message.PayloadJson,
        Status = (int)message.Status, AttemptCount = message.AttemptCount, NextAttemptUtc = message.NextAttemptUtc, LastError = message.LastError,
    };

    private static SyncOutboxMessage ToMessage(JsonSyncOutboxRow row) => new(row.EventId, row.SourceDeviceId, row.Sequence, row.EventType, row.SchemaVersion,
        row.OccurredUtc, row.AggregateType, row.AggregateId, row.PayloadJson, (SyncOutboxStatus)row.Status, row.AttemptCount, row.NextAttemptUtc, row.LastError);
}
