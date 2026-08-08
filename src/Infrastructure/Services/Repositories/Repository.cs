using CoreApp.Interfaces;
using SQLite;
using System.Linq.Expressions;

namespace Infrastructure.Services.Repositories;

public class Repository<T> : IRepositoryExtended<T> where T : new()
{
    private readonly MothballDatabase db;
    private Task? initTask;

    public Repository(MothballDatabase db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => db.InitializeAsync();

    private Task EnsureInitializedAsync()
        => initTask ??= db.InitializeAsync();

    private SQLiteAsyncConnection Connection => db.Connection;

    /// <inheritdoc />
    public async Task<int> InsertAsync(T entity)
    {
        await EnsureInitializedAsync();
        return await Connection.InsertAsync(entity);
    }

    /// <inheritdoc />
    public async Task<int> InsertAllAsync(IEnumerable<T> entities)
    {
        await EnsureInitializedAsync();
        return await Connection.InsertAllAsync(entities);
    }

    /// <inheritdoc />
    public async Task<int> UpdateAsync(T entity)
    {
        await EnsureInitializedAsync();
        return await Connection.UpdateAsync(entity);
    }

    /// <inheritdoc />
    public async Task<int> DeleteAsync(T entity)
    {
        await EnsureInitializedAsync();
        return await Connection.DeleteAsync(entity);
    }

    /// <inheritdoc />
    public async Task<int> UpsertAsync(T entity)
    {
        await EnsureInitializedAsync();
        // sqlite-net InsertOrReplaceAsync is convenient for simple PK entities
        return await Connection.InsertOrReplaceAsync(entity);
    }

    /// <inheritdoc />
    public async Task<T> GetAsync(object primaryKey)
    {
        await EnsureInitializedAsync();
        return await Connection.FindAsync<T>(primaryKey);
    }

    /// <inheritdoc />
    public async Task<List<T>> GetAllAsync()
    {
        await EnsureInitializedAsync();
        return await Connection.Table<T>().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<T>> GetAllAsync(int skip, int take)
    {
        await EnsureInitializedAsync();
        return await Connection.Table<T>().Skip(skip).Take(take).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        await EnsureInitializedAsync();
        return await Connection.Table<T>().FirstOrDefaultAsync(predicate);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        await EnsureInitializedAsync();
        return (await Connection.Table<T>().CountAsync(predicate)) > 0;
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        await EnsureInitializedAsync();
        return await Connection.Table<T>().CountAsync(predicate);
    }

    /// <inheritdoc />
    public async Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate, int skip, int take)
    {
        await EnsureInitializedAsync();
        return await Connection.Table<T>().Where(predicate).Skip(skip).Take(take).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate)
    {
        await EnsureInitializedAsync();
        return await Connection.Table<T>().Where(predicate).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<T>> WhereInAsync(string propertyName, IEnumerable<object> values)
    {
        // Build a dynamic IN clause: SELECT * FROM T WHERE propertyName IN (?, ?, ...)
        var list = values?.ToList() ?? new List<object>();
        if (list.Count == 0)
            return new List<T>();

        await EnsureInitializedAsync();

        // Validate property name against actual public instance properties of T
        var prop = typeof(T).GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.Ordinal));
        if (prop is null)
            throw new ArgumentException($"Property '{propertyName}' does not exist on {typeof(T).Name}.", nameof(propertyName));

        var safePropertyName = prop.Name;

        var placeholders = string.Join(",", Enumerable.Repeat("?", list.Count));
        var table = typeof(T).Name;
        var query = $"SELECT * FROM {table} WHERE {safePropertyName} IN ({placeholders})";

        return await Connection.QueryAsync<T>(query, list.ToArray());
    }

    /// <inheritdoc />
    public async Task<List<T>> QueryAsync(string query, params object[] args)
    {
        await EnsureInitializedAsync();
        return await Connection.QueryAsync<T>(query, args);
    }

    /// <inheritdoc />
    public AsyncTableQuery<T> Table()
    {
        // Caller should have ensured initialization; keep for advanced scenarios
        return Connection.Table<T>();
    }
}
