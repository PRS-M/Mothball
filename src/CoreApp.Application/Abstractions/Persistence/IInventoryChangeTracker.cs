namespace CoreApp.Application.Abstractions.Persistence;

/// <summary>
/// Tracks successful inventory mutations so cached query views can detect stale data.
/// </summary>
public interface IInventoryChangeTracker
{
    /// <summary>Gets the current inventory revision.</summary>
    long Revision { get; }

    /// <summary>Advances the revision after a successful inventory mutation.</summary>
    void MarkChanged();
}
