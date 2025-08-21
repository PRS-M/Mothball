using System.Linq.Expressions;
using SQLite;

namespace MothballMobile.Infrastructure;

/// <summary>
/// Generic repository abstraction for CRUD and common query patterns over a SQLite table using sqlite-net.
/// </summary>
/// <typeparam name="T">POCO that represents a SQLite table row. Must have a parameterless constructor.</typeparam>
public interface IRepository<T> where T : new()
{
    /// <summary>
    /// Ensures the underlying SQLite connection is created and tables are available.
    /// Safe to call multiple times; no-op after the first initialization.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Inserts a single entity into the table.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>The number of rows inserted (1 on success).</returns>
    Task<int> InsertAsync(T entity);

    /// <summary>
    /// Inserts a batch of entities in a single call.
    /// </summary>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <returns>Total rows inserted.</returns>
    Task<int> InsertAllAsync(IEnumerable<T> entities);

    /// <summary>
    /// Updates an existing entity matched by its primary key.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <returns>Rows affected.</returns>
    Task<int> UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity matched by its primary key.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <returns>Rows affected.</returns>
    Task<int> DeleteAsync(T entity);

    /// <summary>
    /// Inserts or replaces an entity based on its primary key.
    /// Uses SQLite REPLACE semantics (DELETE+INSERT) when conflict occurs.
    /// </summary>
    /// <param name="entity">The entity to upsert.</param>
    /// <returns>Rows affected.</returns>
    Task<int> UpsertAsync(T entity);

    /// <summary>
    /// Finds a single entity by its primary key.
    /// </summary>
    /// <param name="primaryKey">Primary key value.</param>
    /// <returns>The entity, or default(T) if not found.</returns>
    Task<T> GetAsync(object primaryKey);

    /// <summary>
    /// Returns all rows from the table.
    /// </summary>
    /// <returns>List of entities.</returns>
    Task<List<T>> GetAllAsync();

    // Predicate-based queries
    /// <summary>
    /// Filters rows by predicate.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <returns>Matching rows.</returns>
    Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Filters rows by predicate with paging.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <param name="skip">Number of rows to skip.</param>
    /// <param name="take">Number of rows to take.</param>
    /// <returns>Matching rows for the requested page.</returns>
    Task<List<T>> WhereAsync(Expression<Func<T, bool>> predicate, int skip, int take);

    /// <summary>
    /// Returns the first row matching the predicate or default(T) if none.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <returns>First matching entity or default(T).</returns>
    Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Returns true if any rows match the predicate.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <returns>True if any rows match.</returns>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Counts rows matching the predicate.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <returns>Count of matching rows.</returns>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Filters on a set of values for a given property by generating a parameterized IN clause.
    /// </summary>
    /// <param name="propertyName">Column/property name to match. This should correspond to the SQLite column.</param>
    /// <param name="values">Values to include in the IN set. Empty set returns an empty list.</param>
    /// <returns>Matching rows.</returns>
    Task<List<T>> WhereInAsync(string propertyName, IEnumerable<object> values);

    /// <summary>
    /// Executes a raw SQL query and materializes entities.
    /// Prefer predicate-based methods when possible.
    /// </summary>
    /// <param name="query">Parameterized SQL.</param>
    /// <param name="args">Parameters corresponding to '?' placeholders.</param>
    /// <returns>List of entities.</returns>
    Task<List<T>> QueryAsync(string query, params object[] args);

    /// <summary>
    /// Provides low-level access to sqlite-net's query builder for advanced scenarios.
    /// Prefer high-level methods in this interface for common cases.
    /// </summary>
    AsyncTableQuery<T> Table();
}
