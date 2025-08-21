using SQLite;
using System.Linq.Expressions;

namespace MothballMobile.Infrastructure;

public class Repository<T> : IRepository<T> where T : new()
{
    private readonly MothballDatabase _db;

    public Repository(MothballDatabase db)
    {
        _db = db;
    }

    public Task InitializeAsync() => _db.InitializeAsync();

    private SQLiteAsyncConnection Connection => _db.Connection;

    public Task<int> InsertAsync(T entity) => Connection.InsertAsync(entity);
    public Task<int> InsertAllAsync(IEnumerable<T> entities) => Connection.InsertAllAsync(entities);

    public Task<int> UpdateAsync(T entity) => Connection.UpdateAsync(entity);
    public Task<int> DeleteAsync(T entity) => Connection.DeleteAsync(entity);
    public async Task<int> UpsertAsync(T entity)
    {
        // sqlite-net InsertOrReplaceAsync is convenient for simple PK entities
        return await Connection.InsertOrReplaceAsync(entity);
    }

    public Task<T> GetAsync(object primaryKey) => Connection.FindAsync<T>(primaryKey);
    public Task<List<T>> GetAllAsync() => Connection.Table<T>().ToListAsync();

    public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => Connection.Table<T>().Where(predicate).FirstOrDefaultAsync();

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => (await Connection.Table<T>().Where(predicate).CountAsync()) > 0;

    public Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        => Connection.Table<T>().Where(predicate).CountAsync();

    public Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate, int skip, int take)
        => Connection.Table<T>().Where(predicate).Skip(skip).Take(take).ToListAsync();

    public Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        => Connection.Table<T>().Where(predicate).ToListAsync();

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

    public Task<List<T>> QueryAsync(string query, params object[] args) => Connection.QueryAsync<T>(query, args);

    public AsyncTableQuery<T> Table() => Connection.Table<T>();
}
