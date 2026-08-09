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

        public void DeleteImagesByOwner(Guid ownerId)
            => connection.DeleteWhere<DbImage>(p => p.OwnerUniqueId == ownerId);

        public void DeleteRelationsByContainer(Guid containerId)
            => connection.DeleteWhere<DbItemContainerRelation>(r => r.ContainerId == containerId);

        public void DeleteRelationsByItem(Guid itemId)
            => connection.DeleteWhere<DbItemContainerRelation>(r => r.ItemId == itemId);

        public void DeleteContainer(Guid containerId)
            => connection.DeleteByPrimaryKey<DbContainer>(containerId);

        public void DeleteItem(Guid itemId)
            => connection.DeleteByPrimaryKey<DbItem>(itemId);
    }
}
