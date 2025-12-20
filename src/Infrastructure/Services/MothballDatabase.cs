using System;
using System.Linq;
using SQLite;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services;

public class MothballDatabase
{
    private SQLiteAsyncConnection? database;
    private readonly string? customPath;
    private readonly object initLock = new();
    private Task? initializeTask;

    public MothballDatabase(string? databasePath = null)
    {
        customPath = databasePath;
    }

    public SQLiteAsyncConnection Connection
    {
        get
        {
            if (database is null)
                throw new InvalidOperationException("Database not initialized. Call InitializeAsync() first.");
            return database;
        }
    }

    public async Task InitializeAsync()
    {
        Task task;
        lock (initLock)
        {
            initializeTask ??= InitializeCoreAsync();
            task = initializeTask;
        }

        await task;
    }

    private async Task InitializeCoreAsync()
    {
        if (database != null) return;

        var path = string.IsNullOrWhiteSpace(customPath)
            ? SQLiteConstants.DatabasePath
            : customPath!;

        var connection = new SQLiteAsyncConnection(path, SQLiteConstants.OpenFlags);

        // IMPORTANT:
        // sqlite-net's CreateTableAsync<T>() will auto-migrate existing tables by issuing ALTER TABLE ADD COLUMN.
        // On some SQLite builds (e.g., MacCatalyst), adding a NOT NULL column during ALTER TABLE can crash.
        // We therefore only call CreateTableAsync for tables that don't exist yet, and we run our own
        // migrations for existing databases.

        if (!await TableExistsAsync(connection, nameof(DbContainer)))
            await connection.CreateTableAsync<DbContainer>();

        if (!await TableExistsAsync(connection, nameof(DbItem)))
            await connection.CreateTableAsync<DbItem>();

        if (!await TableExistsAsync(connection, nameof(DbImage)))
            await connection.CreateTableAsync<DbImage>();

        if (!await TableExistsAsync(connection, nameof(DbItemContainerRelation)))
            await connection.CreateTableAsync<DbItemContainerRelation>();

        // Run migrations/backfills (safe to run repeatedly).
        await EnsureItemDescriptionColumnAsync(connection);
        await EnsureItemContainerRelationQuantityColumnAsync(connection);

        // Only publish the connection after schema is up-to-date.
        database = connection;
    }

    private static Task<int> ExecuteScalarIntAsync(SQLiteAsyncConnection db, string sql, params object[] args)
        => db.ExecuteScalarAsync<int>(sql, args);

    private static async Task<bool> TableExistsAsync(SQLiteAsyncConnection db, string tableName)
    {
        // sqlite_master is stable across SQLite versions.
        var count = await ExecuteScalarIntAsync(
            db,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?;",
            tableName);
        return count > 0;
    }

    private sealed class SqliteColumnInfo
    {
        // Property name must match PRAGMA table_info column name.
        // ReSharper disable once InconsistentNaming
        public string name { get; set; } = string.Empty;
    }

    private static async Task EnsureItemContainerRelationQuantityColumnAsync(SQLiteAsyncConnection db)
    {
        var table = nameof(DbItemContainerRelation);
        var columns = await db.QueryAsync<SqliteColumnInfo>($"PRAGMA table_info({table});");
        if (columns.Count == 0) return;
        var hasQuantity = columns.Any(c => string.Equals(c.name, nameof(DbItemContainerRelation.Quantity), StringComparison.OrdinalIgnoreCase));

        var column = nameof(DbItemContainerRelation.Quantity);
        if (!hasQuantity)
        {
            // Some SQLite builds reject adding NOT NULL columns during ALTER TABLE even when a default is specified.
            // Add as nullable.
            await db.ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {column} INTEGER;");
        }

        // Always backfill (CreateTableAsync may have added it as nullable).
        await db.ExecuteAsync($"UPDATE {table} SET {column} = 1 WHERE {column} IS NULL;");
    }

    private static async Task EnsureItemDescriptionColumnAsync(SQLiteAsyncConnection db)
    {
        var table = nameof(DbItem);
        var columns = await db.QueryAsync<SqliteColumnInfo>($"PRAGMA table_info({table});");
        if (columns.Count == 0) return;

        var hasDescription = columns.Any(c => string.Equals(c.name, nameof(DbItem.Description), StringComparison.OrdinalIgnoreCase));

        var column = nameof(DbItem.Description);
        if (!hasDescription)
        {
            // Some SQLite builds reject adding NOT NULL columns during ALTER TABLE even when a default is specified.
            // Add as nullable.
            await db.ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {column} TEXT;");
        }

        // Always backfill (CreateTableAsync may have added it as nullable).
        await db.ExecuteAsync($"UPDATE {table} SET {column} = '' WHERE {column} IS NULL;");
    }
}
