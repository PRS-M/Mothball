namespace CoreApp.Application.Utilities;

/// <summary>
/// Provides a process-local, thread-safe inventory revision counter.
/// </summary>
public sealed class InventoryChangeTracker : IInventoryChangeTracker
{
    private long revision;

    public long Revision => Interlocked.Read(ref revision);

    public void MarkChanged() => Interlocked.Increment(ref revision);
}
