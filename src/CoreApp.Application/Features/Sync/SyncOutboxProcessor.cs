using CoreApp.Application.Abstractions.Sync;

namespace CoreApp.Application.Features.Sync;

/// <summary>Coordinates retryable delivery of the local synchronization outbox.</summary>
public sealed class SyncOutboxProcessor
{
    private readonly ISyncOutboxStore outbox;
    private readonly ISyncTransport transport;

    public SyncOutboxProcessor(ISyncOutboxStore outbox, ISyncTransport transport)
    {
        this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>Attempts to send one batch of eligible messages.</summary>
    public async Task<int> ProcessBatchAsync(
        int batchSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        var now = DateTimeOffset.UtcNow;
        var messages = await outbox.ClaimPendingAsync(batchSize, now, cancellationToken).ConfigureAwait(false);
        if (messages.Count == 0)
        {
            return 0;
        }

        var eventIds = messages.Select(message => message.EventId).ToArray();
        try
        {
            await transport.SendAsync(messages, cancellationToken).ConfigureAwait(false);
            await outbox.MarkSentAsync(eventIds, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await outbox.MarkFailedAsync(eventIds, exception.Message, now, cancellationToken).ConfigureAwait(false);
            throw;
        }

        return messages.Count;
    }
}
