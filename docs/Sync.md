# External synchronization

The application publishes local domain events into a durable synchronization outbox. The outbox is deliberately transport-neutral so the future web-service client can be added without coupling the domain or repositories to HTTP, authentication, or server DTOs.

Each message contains:

- a stable event ID for server-side idempotency;
- a durable installation/device ID and monotonic local sequence;
- event type and schema version for contract evolution;
- occurrence time, aggregate identity, and serialized event payload;
- delivery status, attempt count, next retry time, and the last error.

SQLite and the JSON operational store both persist the outbox. Claiming a batch changes messages to `InFlight`, successful transport delivery changes them to `Sent`, and failures schedule exponential backoff. The transport must treat `(SourceDeviceId, Sequence)` and `EventId` as idempotency keys because a process can fail after the server accepts a batch but before the local status update.

The current `SyncOutboxEventHandler` runs after the local repository operation has committed. This makes the outbox durable and retryable, but it does not yet provide a strict transactional-outbox guarantee across the inventory write and outbox insert: a crash in that narrow window can lose the event. The web-service integration should close that gap by enqueuing the serialized event in the same SQLite transaction, or by adding a recovery scan/reconciliation protocol before enabling production synchronization.

The remote contract is intentionally not implemented until authentication, conflict policy, server acknowledgements, and pull/cursor semantics are defined. `ISyncTransport` and `SyncOutboxProcessor` are the application seam for that work.
