using System;
using SQLite;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services;

public class MothballDatabase
{
    private SQLiteAsyncConnection? database;
    private readonly string? customPath;

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
        if (database != null) return;

        var path = string.IsNullOrWhiteSpace(customPath)
            ? SQLiteConstants.DatabasePath
            : customPath!;
        database = new SQLiteAsyncConnection(path, SQLiteConstants.OpenFlags);

        // Create tables for DB models
        await database.CreateTableAsync<DbContainer>();
        await database.CreateTableAsync<DbItem>();
        await database.CreateTableAsync<DbImage>();
        await database.CreateTableAsync<DbItemContainerRelation>();
    }
}
