namespace CoreApp.Interfaces;

/// <summary>
/// Domain-oriented repository that composes SQLite entities into rich domain models.
/// </summary>
public interface IInventoryDomainRepository : IInventoryQueryRepository, IInventoryCommandRepository
{
    // Transitional composite contract. New code should prefer IInventoryQueryRepository
    // and IInventoryCommandRepository directly.
}
