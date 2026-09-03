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
    /// Initializes the SQLite connection and creates the current database schema when needed.
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

        // Schema changes during development require resetting the local database.
        await CreateTableIfNotExistsAsync<DbContainer>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbItem>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbItemInventory>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbImage>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbItemContainerRelation>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbWorkspace>(databaseConnection);
        await CreateTableIfNotExistsAsync<DbWorkspaceDefaults>(databaseConnection);

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
