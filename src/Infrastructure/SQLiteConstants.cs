using SQLite;

namespace Infrastructure.Services;

public static class SQLiteConstants
{
    public const string DatabaseName = "mothballmobile.db";
    public const SQLiteOpenFlags OpenFlags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
    public static readonly string DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseName);
}
