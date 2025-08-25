using System;
using SQLite;
using MothballMobile.Infrastructure.DatabaseModels;

namespace MothballMobile.Infrastructure;

public class MothballDatabase
{
    private SQLiteAsyncConnection? _database;

    public SQLiteAsyncConnection Connection
    {
        get
        {
            if (_database is null)
                throw new InvalidOperationException("Database not initialized. Call InitializeAsync() first.");
            return _database;
        }
    }

    public async Task InitializeAsync()
    {
        if (_database != null) return;

        _database = new SQLiteAsyncConnection(SQLiteConstants.DatabasePath, SQLiteConstants.OpenFlags);

        // Create tables for DB models
        await _database.CreateTableAsync<DbContainer>();
        await _database.CreateTableAsync<DbItem>();
        await _database.CreateTableAsync<DbPhoto>();
        await _database.CreateTableAsync<DbItemContainerRelation>();
    }
}
