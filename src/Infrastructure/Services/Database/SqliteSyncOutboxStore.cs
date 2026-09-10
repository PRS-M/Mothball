using CoreApp.Application.Abstractions.Sync;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Database;

/// <summary>SQLite persistence for the local synchronization outbox and device identity.</summary>
public sealed class SqliteSyncOutboxStore : ISyncOutboxStore, ISyncDeviceIdentity
{
    private readonly MothballDatabase database;
    private string? deviceId;

    public SqliteSyncOutboxStore(MothballDatabase database)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public string DeviceId => deviceId ??= Guid.NewGuid().ToString("N");

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken).ConfigureAwait(false);
        var row = await database.Connection.FindAsync<DbSyncDeviceMetadata>(1).ConfigureAwait(false);
        if (row is null)
        {
            row = new DbSyncDeviceMetadata();
            await database.Connection.InsertAsync(row).ConfigureAwait(false);
        }

        deviceId = row.DeviceId;
    }

    public async Task EnqueueAsync(IReadOnlyCollection<SyncOutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            var next = connection.Table<DbSyncOutboxEntry>().ToList()
                .Where(entry => entry.SourceDeviceId == DeviceId)
                .Select(entry => entry.Sequence)
                .DefaultIfEmpty(0)
                .Max() + 1;

            foreach (var message in messages)
            {
                if (connection.Find<DbSyncOutboxEntry>(message.EventId) is not null) continue;
                connection.Insert(ToRow(message with { SourceDeviceId = DeviceId, Sequence = next++ }));
            }
        });
    }

    public async Task<IReadOnlyList<SyncOutboxMessage>> ClaimPendingAsync(int batchSize, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize));
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        var rows = await database.Connection.Table<DbSyncOutboxEntry>().ToListAsync().ConfigureAwait(false);
        var candidates = rows.Where(row => (row.Status == (int)SyncOutboxStatus.Pending || row.Status == (int)SyncOutboxStatus.Failed) &&
                                           (row.NextAttemptUtc is null || Parse(row.NextAttemptUtc) <= nowUtc))
            .OrderBy(row => row.Sequence).Take(batchSize).ToList();
        await database.RunInTransactionAsync(connection =>
        {
            foreach (var row in candidates)
            {
                row.Status = (int)SyncOutboxStatus.InFlight;
                connection.Update(row);
            }
        });
        return candidates.Select(ToMessage).ToList();
    }

    public async Task MarkSentAsync(IReadOnlyCollection<Guid> eventIds, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            foreach (var id in eventIds)
            {
                var row = connection.Find<DbSyncOutboxEntry>(id);
                if (row is null) continue;
                row.Status = (int)SyncOutboxStatus.Sent;
                row.NextAttemptUtc = null;
                row.LastError = null;
                connection.Update(row);
            }
        });
    }

    public async Task MarkFailedAsync(IReadOnlyCollection<Guid> eventIds, string error, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            foreach (var id in eventIds)
            {
                var row = connection.Find<DbSyncOutboxEntry>(id);
                if (row is null) continue;
                row.AttemptCount++;
                row.Status = (int)SyncOutboxStatus.Failed;
                row.NextAttemptUtc = nowUtc.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(row.AttemptCount - 1, 6)))).ToString("O");
                row.LastError = error;
                connection.Update(row);
            }
        });
    }

    private static DbSyncOutboxEntry ToRow(SyncOutboxMessage message) => new()
    {
        EventId = message.EventId, SourceDeviceId = message.SourceDeviceId, Sequence = message.Sequence,
        EventType = message.EventType, SchemaVersion = message.SchemaVersion, OccurredUtc = message.OccurredUtc.ToString("O"),
        AggregateType = message.AggregateType, AggregateId = message.AggregateId, PayloadJson = message.PayloadJson,
        Status = (int)message.Status, AttemptCount = message.AttemptCount, NextAttemptUtc = message.NextAttemptUtc?.ToString("O"), LastError = message.LastError,
    };

    private static SyncOutboxMessage ToMessage(DbSyncOutboxEntry row) => new(row.EventId, row.SourceDeviceId, row.Sequence, row.EventType, row.SchemaVersion,
        Parse(row.OccurredUtc), row.AggregateType, row.AggregateId, row.PayloadJson, (SyncOutboxStatus)row.Status, row.AttemptCount,
        row.NextAttemptUtc is null ? null : Parse(row.NextAttemptUtc), row.LastError);

    private static DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
}
