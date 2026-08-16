namespace Infrastructure.Abstractions.Transactions;

using Infrastructure.Services.DatabaseModels;

/// <summary>
/// Defines transactional execution for inventory persistence operations.
/// </summary>
public interface ITransactionRunner
{
    /// <summary>
    /// Runs the supplied persistence operations in a transaction.
    /// </summary>
    /// <param name="action">The value used by the operation.</param>
    Task RunAsync(Action<ITransactionalDeleteScope> action);
}

/// <summary>
/// Defines the persistence operations available within a transactional delete scope.
/// </summary>
public interface ITransactionalDeleteScope
{
    /// <summary>
    /// Deletes the Image.
    /// </summary>
    /// <param name="imageId">The identifier used by the operation.</param>
    /// <param name="ownerId">The identifier used by the operation.</param>
    void DeleteImage(Guid imageId, Guid ownerId);
    /// <summary>
    /// Deletes the Images By Owner.
    /// </summary>
    /// <param name="ownerId">The identifier used by the operation.</param>
    void DeleteImagesByOwner(Guid ownerId);
    /// <summary>
    /// Deletes the Relations By Container.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    void DeleteRelationsByContainer(Guid containerId);
    /// <summary>
    /// Deletes the Relations By Item.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    void DeleteRelationsByItem(Guid itemId);
    /// <summary>
    /// Updates the Container.
    /// </summary>
    /// <param name="container">The value used by the operation.</param>
    void UpdateContainer(DbContainer container);
    /// <summary>
    /// Updates the Item.
    /// </summary>
    /// <param name="item">The value used by the operation.</param>
    void UpdateItem(DbItem item);
    /// <summary>
    /// Inserts the Or Replace Item Inventory.
    /// </summary>
    /// <param name="inventory">The value used by the operation.</param>
    void InsertOrReplaceItemInventory(DbItemInventory inventory);
    /// <summary>
    /// Deletes the Item Inventory.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    void DeleteItemInventory(Guid itemId);
    /// <summary>
    /// Replaces the Item Container Relation.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    /// <param name="containerId">The identifier used by the operation.</param>
    /// <param name="quantity">The quantity used by the operation.</param>
    void ReplaceItemContainerRelation(Guid itemId, Guid containerId, int quantity);
    /// <summary>
    /// Inserts the Item Container Relation.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    /// <param name="containerId">The identifier used by the operation.</param>
    /// <param name="quantity">The quantity used by the operation.</param>
    void InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity);
    /// <summary>
    /// Deletes the Container.
    /// </summary>
    /// <param name="containerId">The identifier used by the operation.</param>
    void DeleteContainer(Guid containerId);
    /// <summary>
    /// Deletes the Item.
    /// </summary>
    /// <param name="itemId">The identifier used by the operation.</param>
    void DeleteItem(Guid itemId);
}
