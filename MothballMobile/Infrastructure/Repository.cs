using SQLite;

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

    public Task<T> GetAsync(object primaryKey) => Connection.FindAsync<T>(primaryKey);
    public Task<List<T>> GetAllAsync() => Connection.Table<T>().ToListAsync();

    public Task<List<T>> QueryAsync(string query, params object[] args) => Connection.QueryAsync<T>(query, args);

    public AsyncTableQuery<T> Table() => Connection.Table<T>();
}
