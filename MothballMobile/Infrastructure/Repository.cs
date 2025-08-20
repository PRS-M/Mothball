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

    private SQLiteAsyncConnection Conn => _db.Connection;

    public Task<int> InsertAsync(T entity) => Conn.InsertAsync(entity);
    public Task<int> InsertAllAsync(IEnumerable<T> entities) => Conn.InsertAllAsync(entities);

    public Task<int> UpdateAsync(T entity) => Conn.UpdateAsync(entity);
    public Task<int> DeleteAsync(T entity) => Conn.DeleteAsync(entity);

    public Task<T> GetAsync(object primaryKey) => Conn.FindAsync<T>(primaryKey);
    public Task<List<T>> GetAllAsync() => Conn.Table<T>().ToListAsync();

    public Task<List<T>> QueryAsync(string query, params object[] args) => Conn.QueryAsync<T>(query, args);

    public AsyncTableQuery<T> Table() => Conn.Table<T>();
}
