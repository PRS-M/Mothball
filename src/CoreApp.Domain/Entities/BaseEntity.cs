namespace CoreApp.Domain.Entities;

/// <summary>
/// Provides the persistence identifier shared by entities.
/// </summary>
public class BaseEntity
{
    /// <summary>
    /// Gets the persistence identifier for this entity.
    /// </summary>
    public int Id { get; protected set; }
}