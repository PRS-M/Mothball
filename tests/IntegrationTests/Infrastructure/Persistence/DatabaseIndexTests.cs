using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Services;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Repositories;

namespace Mothball.Tests.Integration.Infrastructure.Persistence;

[TestFixture]
public class DatabaseIndexTests
{
    private string dbPath = null!;
    private MothballDatabase db = null!;

    private class IndexListRow
    {
        public int seq { get; set; }
        public string name { get; set; } = string.Empty;
        public int unique { get; set; }
        public string origin { get; set; } = string.Empty;
        public int partial { get; set; }
    }

    private class IndexInfoRow
    {
        public int seqno { get; set; }
        public int cid { get; set; }
        public string name { get; set; } = string.Empty;
    }

    [SetUp]
    public async Task Setup()
    {
        dbPath = Path.Combine(Path.GetTempPath(), $"mothball-index-{Guid.NewGuid():N}.db");
        db = new MothballDatabase(dbPath);
        await db.InitializeAsync();
    }

    [TearDown]
    public async Task Teardown()
    {
        try { if (db != null) await db.DisposeAsync(); }
        catch (Exception ex)
        {
            TestContext.Error.WriteLine($"Failed to dispose test database: {ex}");
        }

        try { if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath)) File.Delete(dbPath); }
        catch (Exception ex)
        {
            TestContext.Error.WriteLine($"Failed to delete test database '{dbPath}': {ex}");
        }
    }

    private async Task<bool> ColumnHasIndexAsync(string table, string column)
    {
        var indexListRepo = new Repository<IndexListRow>(db);
        var indexInfoRepo = new Repository<IndexInfoRow>(db);

        var indexes = await indexListRepo.QueryAsync($"PRAGMA index_list('{table}');");
        foreach (var idx in indexes)
        {
            var cols = await indexInfoRepo.QueryAsync($"PRAGMA index_info('{idx.name}');");
            if (cols.Any(c => string.Equals(c.name, column, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }
        return false;
    }

    private async Task<bool> HasUniqueIndexAsync(string table, params string[] columns)
    {
        var indexListRepo = new Repository<IndexListRow>(db);
        var indexInfoRepo = new Repository<IndexInfoRow>(db);

        var indexes = await indexListRepo.QueryAsync($"PRAGMA index_list('{table}');");
        foreach (var index in indexes.Where(index => index.unique == 1))
        {
            var indexedColumns = await indexInfoRepo.QueryAsync($"PRAGMA index_info('{index.name}');");
            var orderedNames = indexedColumns
                .OrderBy(column => column.seqno)
                .Select(column => column.name);

            if (orderedNames.SequenceEqual(columns, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [Test]
    public async Task ModelAttributes_CreateExpectedIndexes()
    {
        Assert.That(await ColumnHasIndexAsync(nameof(DbItemContainerRelation), nameof(DbItemContainerRelation.ContainerId)), Is.True, "Missing index: relations.ContainerId");
        Assert.That(await ColumnHasIndexAsync(nameof(DbItemContainerRelation), nameof(DbItemContainerRelation.ItemId)), Is.True, "Missing index: relations.ItemId");
        Assert.That(await ColumnHasIndexAsync(nameof(DbImage), nameof(DbImage.OwnerUniqueId)), Is.True, "Missing index: images.OwnerUniqueId");
        Assert.That(await ColumnHasIndexAsync(nameof(DbItem), nameof(DbItem.Name)), Is.True, "Missing index: items.Name");
        Assert.That(await ColumnHasIndexAsync(nameof(DbContainer), nameof(DbContainer.Name)), Is.True, "Missing index: containers.Name");
        Assert.That(
            await HasUniqueIndexAsync(
                nameof(DbItemContainerRelation),
                nameof(DbItemContainerRelation.ItemId),
                nameof(DbItemContainerRelation.ContainerId)),
            Is.True,
            "Missing unique index: relations.ItemId + relations.ContainerId");
    }
}
