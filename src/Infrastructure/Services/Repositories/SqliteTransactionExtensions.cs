using System.Linq.Expressions;
using SQLite;

namespace Infrastructure.Services.Repositories;

internal static class SqliteTransactionExtensions
{
    public static void DeleteWhere<T>(this SQLiteConnection connection, Expression<Func<T, bool>> predicate) where T : new()
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(predicate);

        var rows = connection.Table<T>().Where(predicate).ToList();
        foreach (var row in rows)
        {
            connection.Delete(row);
        }
    }

    public static void DeleteByPrimaryKey<T>(this SQLiteConnection connection, object primaryKey) where T : new()
    {
        ArgumentNullException.ThrowIfNull(connection);

        var row = connection.Find<T>(primaryKey);
        if (row is not null)
        {
            connection.Delete(row);
        }
    }
}
