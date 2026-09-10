using CoreApp.Application.Abstractions.Sync;
using CoreApp.Application.Features.Sync;

namespace Mothball.Tests.Unit.Core.Sync;

[TestFixture]
public sealed class SyncOutboxProcessorTests
{
    [Test]
    public async Task ProcessBatch_SendsClaimedMessagesAndMarksThemSent()
    {
        var message = CreateMessage();
        var store = new RecordingStore([message]);
        var transport = new RecordingTransport();

        var processed = await new SyncOutboxProcessor(store, transport).ProcessBatchAsync();

        Assert.That(processed, Is.EqualTo(1));
        Assert.That(transport.Messages.Single().EventId, Is.EqualTo(message.EventId));
        Assert.That(store.Sent, Is.EqualTo(new[] { message.EventId }));
    }

    [Test]
    public void ProcessBatch_WhenTransportFails_RecordsFailureAndRethrows()
    {
        var message = CreateMessage();
        var store = new RecordingStore([message]);
        var transport = new RecordingTransport { Exception = new InvalidOperationException("offline") };

        Assert.ThrowsAsync<InvalidOperationException>(() => new SyncOutboxProcessor(store, transport).ProcessBatchAsync());
        Assert.That(store.Failed.Single().Ids, Is.EqualTo(new[] { message.EventId }));
        Assert.That(store.Failed.Single().Error, Is.EqualTo("offline"));
    }

    private static SyncOutboxMessage CreateMessage() => new(Guid.NewGuid(), "device", 1, "ItemCreated", 1,
        DateTimeOffset.UtcNow, "Item", Guid.NewGuid(), "{}");

    private sealed class RecordingStore(IReadOnlyList<SyncOutboxMessage> pending) : ISyncOutboxStore
    {
        public List<Guid> Sent { get; } = [];
        public List<(IReadOnlyCollection<Guid> Ids, string Error)> Failed { get; } = [];

        public Task EnqueueAsync(IReadOnlyCollection<SyncOutboxMessage> messages, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<SyncOutboxMessage>> ClaimPendingAsync(int batchSize, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SyncOutboxMessage>>(pending.Take(batchSize).ToList());

        public Task MarkSentAsync(IReadOnlyCollection<Guid> eventIds, CancellationToken cancellationToken = default)
        {
            Sent.AddRange(eventIds);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(IReadOnlyCollection<Guid> eventIds, string error, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
        {
            Failed.Add((eventIds, error));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingTransport : ISyncTransport
    {
        public List<SyncOutboxMessage> Messages { get; } = [];
        public Exception? Exception { get; init; }

        public Task SendAsync(IReadOnlyCollection<SyncOutboxMessage> messages, CancellationToken cancellationToken = default)
        {
            if (Exception is not null) throw Exception;
            Messages.AddRange(messages);
            return Task.CompletedTask;
        }
    }
}
