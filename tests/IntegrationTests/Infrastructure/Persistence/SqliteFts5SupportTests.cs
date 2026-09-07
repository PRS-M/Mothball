using SQLite;

namespace Mothball.Tests.Integration.Infrastructure.Persistence;

[TestFixture]
public class SqliteFts5SupportTests
{
    private string dbPath = null!;
    private MothballDatabase database = null!;

    [SetUp]
    public async Task SetUp()
    {
        dbPath = Path.Combine(Path.GetTempPath(), $"mothball-fts5-{Guid.NewGuid():N}.db");
        database = new MothballDatabase(dbPath);
        await database.InitializeAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await database.DisposeAsync();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    [Test]
    public async Task Provider_ReportsWhetherFts5IsAvailable()
    {
        const string tableName = "__mothball_fts5_probe";

        try
        {
            await database.Connection.ExecuteAsync(
                $"CREATE VIRTUAL TABLE {tableName} USING fts5(Content)");
            await database.Connection.ExecuteAsync(
                $"INSERT INTO {tableName} (Content) VALUES (?)",
                "storage box");

            var matches = await database.Connection.QueryAsync<FtsProbeRow>(
                $"SELECT Content FROM {tableName} WHERE {tableName} MATCH ?",
                "storage");

            Assert.That(matches.Select(match => match.Content), Is.EqualTo(new[] { "storage box" }));

            // FTS5 matches tokens; it does not preserve the substring semantics
            // currently exposed by the inventory LIKE-based search.
            var substringMatches = await database.Connection.QueryAsync<FtsProbeRow>(
                $"SELECT Content FROM {tableName} WHERE {tableName} MATCH ?",
                "tor");

            Assert.That(substringMatches, Is.Empty);
        }
        catch (SQLiteException exception) when (exception.Message.Contains("no such module", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore($"The native SQLite provider does not include FTS5: {exception.Message}");
        }
        finally
        {
            await database.Connection.ExecuteAsync($"DROP TABLE IF EXISTS {tableName}");
        }
    }

    private sealed class FtsProbeRow
    {
        public string Content { get; set; } = string.Empty;
    }
}
