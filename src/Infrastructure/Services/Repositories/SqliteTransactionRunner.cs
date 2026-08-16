using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;
using SQLite;

namespace Infrastructure.Services.Repositories;

public sealed class SqliteTransactionRunner : ITransactionRunner
{
    private readonly MothballDatabase database;

    public SqliteTransactionRunner(MothballDatabase database)
    {
        this.database = database;
    }

    public async Task RunAsync(Action<ITransactionalDeleteScope> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await database.RunInTransactionAsync(connection =>
        {
            var scope = new SqliteTransactionalDeleteScope(connection);
            action(scope);
        });
    }

    private sealed class SqliteTransactionalDeleteScope : ITransactionalDeleteScope
    {
        private readonly SQLiteConnection connection;

        public SqliteTransactionalDeleteScope(SQLiteConnection connection)
        {
            this.connection = connection;
        }

        public void DeleteImage(Guid imageId, Guid ownerId)
            => connection.DeleteWhere<DbImage>(p => p.ImageId == imageId && p.OwnerUniqueId == ownerId);

        public void DeleteImagesByOwner(Guid ownerId)
            => connection.DeleteWhere<DbImage>(p => p.OwnerUniqueId == ownerId);

        public void DeleteRelationsByContainer(Guid containerId)
            => connection.DeleteWhere<DbItemContainerRelation>(r => r.ContainerId == containerId);

        public void DeleteRelationsByItem(Guid itemId)
            => connection.DeleteWhere<DbItemContainerRelation>(r => r.ItemId == itemId);

        public void UpdateContainer(DbContainer container)
            => connection.Update(container);

        public void UpdateItem(DbItem item)
            => connection.Update(item);

        public void InsertOrReplaceItemInventory(DbItemInventory inventory)
            => connection.InsertOrReplace(inventory);

        public void DeleteItemInventory(Guid itemId)
            => connection.DeleteByPrimaryKey<DbItemInventory>(itemId);

        public void ReplaceItemContainerRelation(Guid itemId, Guid containerId, int quantity)
        {
            connection.DeleteWhere<DbItemContainerRelation>(
                relation => relation.ItemId == itemId && relation.ContainerId == containerId);

            if (quantity > 0)
            {
                connection.Insert(new DbItemContainerRelation
                {
                    ItemId = itemId,
                    ContainerId = containerId,
                    Quantity = quantity,
                });
            }
        }

        public void InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity)
        {
            var existingQuantity = connection.Table<DbItemContainerRelation>()
                .Where(relation => relation.ItemId == itemId && relation.ContainerId == containerId)
                .ToList()
                .Sum(relation => relation.Quantity);

            ReplaceItemContainerRelation(itemId, containerId, existingQuantity + quantity);
        }

        public void DeleteContainer(Guid containerId)
            => connection.DeleteByPrimaryKey<DbContainer>(containerId);

        public void DeleteItem(Guid itemId)
        {
            DeleteItemInventory(itemId);
            connection.DeleteByPrimaryKey<DbItem>(itemId);
        }
    }
}
