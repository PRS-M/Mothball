using CoreApp.Interfaces;
using SQLite;
using System.Linq.Expressions;

namespace MothballMobile.Infrastructure;

public class Repository<T> : IRepositoryExtended<T> where T : new()
{
    private readonly MothballDatabase _db;

    public Repository(MothballDatabase db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => _db.InitializeAsync();

    private SQLiteAsyncConnection Connection => _db.Connection;

    /// <inheritdoc />
    public Task<int> InsertAsync(T entity) => Connection.InsertAsync(entity);

    /// <inheritdoc />
    public Task<int> InsertAllAsync(IEnumerable<T> entities) => Connection.InsertAllAsync(entities);

    /// <inheritdoc />
    public Task<int> UpdateAsync(T entity) => Connection.UpdateAsync(entity);

    /// <inheritdoc />
    public Task<int> DeleteAsync(T entity) => Connection.DeleteAsync(entity);

    /// <inheritdoc />
    public async Task<int> UpsertAsync(T entity)
    {
        // sqlite-net InsertOrReplaceAsync is convenient for simple PK entities
        return await Connection.InsertOrReplaceAsync(entity);
    }

    /// <inheritdoc />
    public Task<T> GetAsync(object primaryKey) => Connection.FindAsync<T>(primaryKey);

    /// <inheritdoc />
    public Task<List<T>> GetAllAsync() => Connection.Table<T>().ToListAsync();

    /// <inheritdoc />
    public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => Connection.Table<T>().FirstOrDefaultAsync(predicate);

    /// <inheritdoc />
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => (await Connection.Table<T>().CountAsync(predicate)) > 0;

    /// <inheritdoc />
    public Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        => Connection.Table<T>().CountAsync(predicate);

    /// <inheritdoc />
    public Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate, int skip, int take)
        => Connection.Table<T>().Where(predicate).Skip(skip).Take(take).ToListAsync();

    /// <inheritdoc />
    public Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        => Connection.Table<T>().Where(predicate).ToListAsync();

    /// <inheritdoc />
    public Task<List<T>> WhereInAsync(string propertyName, IEnumerable<object> values)
    {
        // Build a dynamic IN clause: SELECT * FROM T WHERE propertyName IN (?, ?, ...)
        var list = values?.ToList() ?? new List<object>();
        if (list.Count == 0)
            return Task.FromResult(new List<T>());

        var placeholders = string.Join(",", Enumerable.Repeat("?", list.Count));
        var table = typeof(T).Name;
        var query = $"SELECT * FROM {table} WHERE {propertyName} IN ({placeholders})";

        return Connection.QueryAsync<T>(query, list.ToArray());
    }

    /// <inheritdoc />
    public Task<List<T>> QueryAsync(string query, params object[] args) => Connection.QueryAsync<T>(query, args);

    /// <inheritdoc />
    public AsyncTableQuery<T> Table() => Connection.Table<T>();
}
