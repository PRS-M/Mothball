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
    Task<int> UpsertAsync(T entity);

    Task<T> GetAsync(object primaryKey);
    Task<List<T>> GetAllAsync();

    // Predicate-based queries
    Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate, int skip, int take);
    Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> WhereInAsync(string propertyName, IEnumerable<object> values);

    Task<List<T>> QueryAsync(string query, params object[] args);
    AsyncTableQuery<T> Table();
}
