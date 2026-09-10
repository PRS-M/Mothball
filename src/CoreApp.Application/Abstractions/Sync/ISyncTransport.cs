namespace CoreApp.Application.Abstractions.Sync;

/// <summary>Uploads local outbox messages to the remote synchronization service.</summary>
public interface ISyncTransport
{
    /// <summary>Uploads a batch and returns only after the server has durably accepted it.</summary>
    Task SendAsync(
        IReadOnlyCollection<SyncOutboxMessage> messages,
        CancellationToken cancellationToken = default);
}
