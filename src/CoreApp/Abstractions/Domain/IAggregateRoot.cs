using System;

namespace CoreApp.Abstractions.Domain;

/// <summary>
/// Marker interface that identifies domain entities as aggregate roots in Domain-Driven Design.
/// Aggregate roots are the only entities that should be directly accessed by repositories
/// and serve as the entry point to their aggregate boundary.
/// </summary>
public interface IAggregateRoot
{
    // Marker interface for aggregate roots
}
