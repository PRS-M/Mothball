using SQLite;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Database;

public class MothballDatabase : IAsyncDisposable
{
    private readonly string databasePath;
    private readonly SemaphoreSlim initLock = new(1, 1);
    private SQLiteAsyncConnection? connection;
    private bool disposed;

    public MothballDatabase(string? databasePath = null)
    {
        this.databasePath = string.IsNullOrWhiteSpace(databasePath)
            ? SQLiteConstants.DatabasePath
            : databasePath;
    }

    public SQLiteAsyncConnection Connection =>
        connection ?? throw new InvalidOperationException("Database not initialized. Call InitializeAsync() first.");

    /// <summary>
    /// Initializes the SQLite connection, creates required tables, and applies database migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (connection != null) return;

        await initLock.WaitAsync();
        try
        {
            if (connection != null) return;
            connection = await InitializeCoreAsync();
        }
        finally
        {
            initLock.Release();
        }
    }

    /// <summary>
    /// Executes database operations in a SQLite transaction.
    /// </summary>
    /// <param name="transactionBody">The operations to execute within the transaction.</param>
    public async Task RunInTransactionAsync(Action<SQLiteConnection> transactionBody)
    {
        ArgumentNullException.ThrowIfNull(transactionBody);

        await InitializeAsync();
        await Connection.RunInTransactionAsync(transactionBody);
    }

    private async Task<SQLiteAsyncConnection> InitializeCoreAsync()
    {
        var databaseConnection = new SQLiteAsyncConnection(databasePath, SQLiteConstants.OpenFlags);

        // Create tables only if they don't exist (avoids MacCatalyst ALTER TABLE NOT NULL crash)
        await CreateTableIfNotExistsAsync<DbContainer>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbItem>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbItemInventory>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbImage>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbItemContainerRelation>(databaseConnection);

        // Run migrations (safe to run repeatedly)
        await EnsureColumnAsync(databaseConnection, nameof(DbItem), nameof(DbItem.Description), "TEXT", "''");
        await EnsureColumnAsync(databaseConnection, nameof(DbItemContainerRelation), nameof(DbItemContainerRelation.Quantity), "INTEGER", "1");
        await EnsureUniqueItemContainerRelationsAsync(databaseConnection);

        return databaseConnection;
    }

    private static async Task CreateTableIfNotExistsAsync<T>(SQLiteAsyncConnection db) where T : new()
    {
        var exists = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?;",
            typeof(T).Name);

        if (exists == 0)
            await db.CreateTableAsync<T>();
    }

    private static async Task<bool> EnsureColumnAsync(
        SQLiteAsyncConnection db, string table, string column, string sqlType, string defaultValue)
    {
        var columns = await db.QueryAsync<ColumnInfo>($"PRAGMA table_info({table});");
        if (columns.Count == 0) return false;

        var hasColumn = columns.Exists(c =>
            string.Equals(c.name, column, StringComparison.OrdinalIgnoreCase));

        bool addedColumn = !hasColumn;
        if (addedColumn)
            await db.ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {column} {sqlType};");

        // Always backfill nulls
        await db.ExecuteAsync($"UPDATE {table} SET {column} = {defaultValue} WHERE {column} IS NULL;");
        return addedColumn;
    }

    private static async Task EnsureUniqueItemContainerRelationsAsync(SQLiteAsyncConnection db)
    {
        const string table = nameof(DbItemContainerRelation);
        const string itemId = nameof(DbItemContainerRelation.ItemId);
        const string containerId = nameof(DbItemContainerRelation.ContainerId);
        const string quantity = nameof(DbItemContainerRelation.Quantity);
        const string id = nameof(DbItemContainerRelation.Id);

        await db.ExecuteAsync(
            $@"UPDATE {table}
               SET {quantity} = (
                   SELECT SUM(source.{quantity})
                   FROM {table} source
                   WHERE source.{itemId} = {table}.{itemId}
                     AND source.{containerId} = {table}.{containerId})
               WHERE {id} IN (
                   SELECT MIN(grouped.{id})
                   FROM {table} grouped
                   GROUP BY grouped.{itemId}, grouped.{containerId});");

        await db.ExecuteAsync(
            $@"DELETE FROM {table}
               WHERE {id} NOT IN (
                   SELECT MIN(grouped.{id})
                   FROM {table} grouped
                   GROUP BY grouped.{itemId}, grouped.{containerId});");

        await db.ExecuteAsync(
            $@"CREATE UNIQUE INDEX IF NOT EXISTS UX_{table}_{itemId}_{containerId}
               ON {table}({itemId}, {containerId});");
    }

    private sealed class ColumnInfo
    {
        public string name { get; set; } = string.Empty;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;

        if (connection != null)
        {
            await connection.CloseAsync();
        }

        initLock.Dispose();
    }
}
