using System;
using SQLite;
using MothballMobile.Infrastructure.DatabaseModels;

namespace MothballMobile.Infrastructure;

public class MothballDatabase
{
    private SQLiteAsyncConnection? database;

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

        database = new SQLiteAsyncConnection(SQLiteConstants.DatabasePath, SQLiteConstants.OpenFlags);

        // Create tables for DB models
        await database.CreateTableAsync<DbContainer>();
        await database.CreateTableAsync<DbItem>();
        await database.CreateTableAsync<DbImage>();
        await database.CreateTableAsync<DbItemContainerRelation>();
    }
}
