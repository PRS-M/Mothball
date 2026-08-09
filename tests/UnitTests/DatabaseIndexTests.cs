using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Infrastructure.Services;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Repositories;

namespace UnitTests;

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
        try { if (db != null) await db.DisposeAsync(); } catch { }
        try { if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath)) File.Delete(dbPath); } catch { }
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

    [Test]
    public async Task ModelAttributes_CreateExpectedIndexes()
    {
        (await ColumnHasIndexAsync(nameof(DbItemContainerRelation), nameof(DbItemContainerRelation.ContainerId))).Should().BeTrue("Missing index: relations.ContainerId");
        (await ColumnHasIndexAsync(nameof(DbItemContainerRelation), nameof(DbItemContainerRelation.ItemId))).Should().BeTrue("Missing index: relations.ItemId");
        (await ColumnHasIndexAsync(nameof(DbImage), nameof(DbImage.OwnerUniqueId))).Should().BeTrue("Missing index: images.OwnerUniqueId");
        (await ColumnHasIndexAsync(nameof(DbItem), nameof(DbItem.Name))).Should().BeTrue("Missing index: items.Name");
        (await ColumnHasIndexAsync(nameof(DbContainer), nameof(DbContainer.Name))).Should().BeTrue("Missing index: containers.Name");
    }
}
