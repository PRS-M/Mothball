using System.Linq.Expressions;
using SQLite;

namespace MothballMobile.Infrastructure;

public interface IRepository<T> where T : new()
{
    Task InitializeAsync();

    Task<int> InsertAsync(T entity);
    Task<int> InsertAllAsync(IEnumerable<T> entities);

    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(T entity);

    Task<T> GetAsync(object primaryKey);
    Task<List<T>> GetAllAsync();

    Task<List<T>> QueryAsync(string query, params object[] args);
    AsyncTableQuery<T> Table();
}
